using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using pe.itsight.Util;
using MongoDB.Driver;
using pe.itsight.Entities;
using MongoDB.Bson;
using System.Diagnostics;
using Tweetinvi.Parameters;
using Tweetinvi.Models;
using System.Text.RegularExpressions;
using System.IO;
using pe.itsight.UserProfile;

namespace pe.itsight.apismartia
{
    public class TaskCore
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void ProcesarAnalisis()
        { 
            string TwitterConsumerKey = ConfigurationManager.AppSettings.Get("TwitterConsumerKey");
            string TwitterConsumerSecret = ConfigurationManager.AppSettings.Get("TwitterConsumerSecret");
            string TwitterAccessToken = ConfigurationManager.AppSettings.Get("TwitterAccessToken");
            string TwitterAccessSecret = ConfigurationManager.AppSettings.Get("TwitterAccessSecret");

            Auth.SetUserCredentials(TwitterConsumerKey, TwitterConsumerSecret, TwitterAccessToken, TwitterAccessSecret);

            TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;

            _log.Info("Iniciando proceso de recolección de datos");

            MongoAsync().Wait();
            var db = MongoAsync().Result;
            _log.Info("Obteniendo datos para Analizar de la BD");

            if (CollectionExists(db, "analysis"))
            {
                var collection = db.GetCollection<Analysis>("analysis");
                var list = collection.AsQueryable<Analysis>().ToList();
                if (list.Count > 0)
                {
                    var analisisRegistrados = list.Where(d => d.stateAnalysis == (int)EstadoAnalisis.Registrado).ToList();

                    Stopwatch sw = new Stopwatch();

                    foreach (var analisis in analisisRegistrados)
                    {
                        _log.InfoFormat("Procesando analisis con id {0} y palabra clave {1}", analisis.id, analisis.nameKey);
                        _log.Info("Cambiando el estado del análisis a DatosRecolectados");
                        UpdateState(db, analisis.id, (int)EstadoAnalisis.DatosRecolectados);
                         
                        sw.Start();
                        _log.Info("Procesando tweets");
                        ProcesarTweets(db, analisis.id, analisis.nameKey);

                        sw.Stop();
                        _log.InfoFormat("Milisegundos totales para los datos recolectados {0}", sw.ElapsedMilliseconds);


                        sw.Start();
                        _log.Info("Cambiando el estado del análisis a AnalisisSemantico");
                        UpdateState(db, analisis.id, (int)EstadoAnalisis.AnalisisSemantico);

                        _log.Info("Procesando el análisis semántico de los tweets");
                        ProcesarAnalisisSemantico(db, analisis.id);
                         
                        //_log.Info("Calculando el sexo de las personas");
                        //RegistroSexoUsuarios(db, analisis.id);


                        _log.Info("Cambiando el estado del análisis a Completado");
                        UpdateState(db, analisis.id, (int)EstadoAnalisis.Completado);

                        UpdateEndDate(db, analisis.id);

                        sw.Stop();
                        Console.WriteLine(sw.ElapsedMilliseconds);
                    }

                }
            }
        }

        public async void UpdateState (IMongoDatabase db, ObjectId id , int state)
        {
            var collection = db.GetCollection<Analysis>("analysis");
            await collection.FindOneAndUpdateAsync(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("stateAnalysis", state));
        }

        public async void UpdateEndDate(IMongoDatabase db, ObjectId id)
        {
            var collection = db.GetCollection<Analysis>("analysis");
            await collection.FindOneAndUpdateAsync(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("endDateAnalysis", DateTime.Now));
        }

        public void ProcesarTweets(IMongoDatabase db, ObjectId id, string palabraClave)
        {
            var listaProcesos = new List<TweetSearchFilters>();
            listaProcesos.Add(TweetSearchFilters.None);
            listaProcesos.Add(TweetSearchFilters.Hashtags);
            listaProcesos.Add(TweetSearchFilters.Replies);
            listaProcesos.Add(TweetSearchFilters.News);

            string jsonTweet = string.Empty;

            var collection = db.GetCollection<Analysis>("analysis");
            var detailsItem = new List<AnalysisItem>();
            var listaFinalhashtags = new List<string>();
            var usersItem = new List<UserTwitter>();
            var hashtagsItem = new List<AnalysisHashtag>();


            foreach (var filter in listaProcesos)
            {
                #region RECOLECCIÓN DE DATOS
                _log.Info("Procesando tweets "+ (filter == TweetSearchFilters.None ? "Sin Clasificar " : 
                                                 filter == TweetSearchFilters.Hashtags ? "Con Hashtags" : 
                                                 filter == TweetSearchFilters.Replies ? "Asociados a Respuestas" :
                                                 filter == TweetSearchFilters.News ? "Asociados a Noticias" : ""));

                var searchParameter = new SearchTweetsParameters(palabraClave)
                {
                    Lang = LanguageFilter.Spanish,
                    SearchType = SearchResultType.Mixed,
                    MaximumNumberOfResults = int.Parse(ConfigurationManager.AppSettings["MaxResults"]),
                    Until = DateTime.Now.AddDays(+1),
                    Since = DateTime.Now.AddDays(-7),
                    Filters = filter
                };

                var tweets = Search.SearchTweets(searchParameter);
                _log.InfoFormat("Total de registros obtenidos: {0}", tweets.Count());

                tweets = (from a in tweets
                          orderby a.CreatedAt
                          select a).ToList();

                if (tweets != null && tweets.Count() != 0)
                {
                    tweets = tweets.OrderBy(x => x.TweetDTO.CreatedAt).ToList();

                    foreach (var item in tweets)
                    {
                        #region PROCESO
                        try
                        {
                            jsonTweet = string.Empty;

                            var analysisItems = new AnalysisItem()
                            {
                                AnalisisId = id,
                                FlagProcesado = false,
                                OrigenAnalisisId = (int)OrigenAnalisis.Twitter,
                                ObjDetalle = jsonTweet,
                                ObjId = item.Id,
                                ObjDetalleAPI = item.ToJson(),
                                TweetOrigen = filter == TweetSearchFilters.Replies ? (int)OrigenTweet.Replies : filter == TweetSearchFilters.News ? (int)OrigenTweet.OriginalPost : item.IsRetweet ? (int)OrigenTweet.Retweet : (int)OrigenTweet.OriginalPost,
                                TweetScreenName = item.CreatedBy.ScreenName,
                                TweetSource = item.Source,
                                TweetTotalFavoritos = item.FavoriteCount,
                                TweetTotalReteweet = item.RetweetCount,
                                TweetUsuarioId = item.CreatedBy.IdStr,
                                TweetFecha = item.CreatedAt,
                                TweetFullText = (string.IsNullOrWhiteSpace(item.FullText) ? Regex.Replace(item.FullText, @"http[^\s]+", "") : Regex.Replace(item.Text, @"http[^\s]+", "")),
                                TweetSearchFiltersId = (int)filter,
                            };

                            detailsItem.Add(analysisItems);

                            var hashtags = item.Hashtags.Select(x => x.Text).ToList();
                            listaFinalhashtags.AddRange(hashtags);

                            var usuario = item.CreatedBy;
                            var jsonObjeto = Newtonsoft.Json.JsonConvert.SerializeObject(usuario);

                            var usuarioBase = new UserTwitter()
                            {
                                Avatar = usuario.ProfileImageUrlHttps,
                                CuentaVerificada = usuario.Verified,
                                Descripcion = usuario.Description,
                                Nombre = usuario.Name,
                                Sexo = (int)Sexo.NoProcesado,
                                TotalSeguidores = usuario.FollowersCount,
                                TweetScreenName = usuario.ScreenName,
                                TweetUsuarioId = usuario.IdStr,
                                ObjDetalleAPI = jsonObjeto
                            };

                            var existe = usersItem.Where(d => d.TweetUsuarioId == usuario.IdStr).FirstOrDefault();
                            if (existe == null)
                            {
                                usersItem.Add(usuarioBase);
                            }
                            else
                            {
                                existe.Avatar = usuario.ProfileImageUrlHttps;
                                existe.CuentaVerificada = usuario.Verified;
                                existe.Descripcion = usuario.Description;
                                existe.Nombre = usuario.Name;
                                existe.TotalSeguidores = usuario.FollowersCount;
                                existe.TweetScreenName = usuario.ScreenName;
                                existe.TweetUsuarioId = usuario.IdStr;
                                existe.ObjDetalleAPI = jsonObjeto;
                            }
                        }

                        catch (Exception ex)
                        {
                            _log.Error(ex.Message, ex);
                        }
                        #endregion
                    }
                }

                #endregion  RECOLECCIÓN DE DATOS
            }

            var listHashtags = listaFinalhashtags.Count == 0 ? new List<string>() : string.Join(",",listaFinalhashtags).Split(',').Select(f => f).Distinct().ToList();
            var listAllHashtags = listaFinalhashtags.Count == 0 ? new List<string>() : string.Join(",", listaFinalhashtags).Split(',').Select(f => f).ToList();

            foreach (var item in listHashtags)
            {
                var total = listAllHashtags.Where(d => d == item).Count();
                var nuevoAnalysisHashtag = new AnalysisHashtag() { Hashtag = item, AnalisisId = id, Total = total };
                hashtagsItem.Add(nuevoAnalysisHashtag);
            }

            collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listDetails", detailsItem));
            collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listHashtags", hashtagsItem));
            collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listUser", usersItem));

        }

        public void ProcesarAnalisisSemantico(IMongoDatabase db, ObjectId id)
        { 
            var proveedor = int.Parse(ConfigurationManager.AppSettings["Sentiment.Provider"]);

            var azure = new Sentiment.AzureTextAnalytics();
            var watson = new Sentiment.IBMWatsonNLU();
            var response = new Sentiment.SentimentResponse();

            watson.JsonBody = File.ReadAllText(ConfigurationManager.AppSettings["WatsonNLU.Input"]);

            var Authorization = "";
            var ApiKey = ConfigurationManager.AppSettings["WatsonNLU.ApiKey"];
            var Password = ConfigurationManager.AppSettings["WatsonNLU.Password"];

            Authorization = "Basic YXBpa2V5Omd1WEQtRWdoZDlTZkUyOTB2dDlqUk02WWVXSk5ZdTJUNWFRcUJaMHg1YmJB";// + (Convert.FromBase64String(ApiKey + " : " + Password).ToString());

            watson.Authorization = Authorization;
            watson.ContentType = ConfigurationManager.AppSettings["WatsonNLU.ContentType"];
            watson.Url = ConfigurationManager.AppSettings["WatsonNLU.URL"];

            var collection = db.GetCollection<Analysis>("analysis");

            var tweets = collection.Find(d => d.id == id).FirstOrDefault();
            _log.InfoFormat("Análisis semántico con identificador {0}", id);

            var listDetails = tweets.listDetails;
            var detailsItem = new List<AnalysisItem>();

            var listaNuevaKeyWords = new List<AnalysisKeyword>();

            foreach (var tweet in listDetails)
            {
                try
                { 
                    if (!tweet.SentimentId.HasValue)
                    { 
                        response = watson.GetSentiment(tweet.TweetFullText, id.ToString());
                        
                        tweet.SentimentId = (int)response.SentimientoId;
                        tweet.SentimentLabel = RetornarSentimentLabel((int)response.SentimientoId);
                        tweet.SentimentScore = response.Score;
                        tweet.MotorAI = proveedor;
                        
                        var lista = response.Keywords;
                        
                        var listaDistinct = lista.Distinct().ToList();
                        foreach (var item in listaDistinct)
                        {
                            var nuevoakeyword = new AnalysisKeyword();
                            nuevoakeyword.AnalisisId = id;
                            nuevoakeyword.Keyword = item;
                            nuevoakeyword.Total = lista.Where(d => d == item).Count();
                            listaNuevaKeyWords.Add(nuevoakeyword);
                        }    
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, ex);
                }

                detailsItem.Add(tweet);
            }


            collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listKeys", listaNuevaKeyWords));
            collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listDetails", detailsItem));

            
        }

        public void RegistroSexoUsuarios(IMongoDatabase db, ObjectId id)
        {
            var genderAPI = new GenderManager();
            var collection = db.GetCollection<Analysis>("analysis");
            var analysis = collection.Find(d => d.id == id).FirstOrDefault();
            if(analysis != null)
            {
                var newlistUser = new List<UserTwitter>();
                var listalluser = analysis.listUser;
                var listalluserdistinct = analysis.listUser.Select(d => d.Nombre).Distinct().ToList();

                foreach (var item in listalluserdistinct)
                {
                    var sexoCalculado = genderAPI.GetGender(item);

                    var listfilter = listalluser.Where(d => d.Nombre == item && d.Sexo == (int)Sexo.NoProcesado).ToList();
                    foreach (var filter in listfilter)
                    {
                        filter.Sexo = sexoCalculado;
                        newlistUser.Add(filter);
                    }
                }

                collection.FindOneAndUpdate(Builders<Analysis>.Filter.Eq("id", id), Builders<Analysis>.Update.Set("listUser", newlistUser));
            }
        }

        private string RetornarSentimentLabel(int sentimientoId)
        {
            switch (sentimientoId)
            {
                case (int)Sentimiento.Error: return "error";
                case (int)Sentimiento.Negativo: return "negative";
                case (int)Sentimiento.Neutral: return "neutral";
                case (int)Sentimiento.Positivo: return "positive";
                default: return "not determined";
            }
        }

        public bool CollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };
            return database.ListCollectionNames(options).Any();
        }

        async Task<IMongoDatabase> MongoAsync()
        {
            string urlMongo = "mongodb://localhost";
            var cliente = new MongoClient(urlMongo);
            return cliente.GetDatabase("ARMongo");
        }

    }
}
