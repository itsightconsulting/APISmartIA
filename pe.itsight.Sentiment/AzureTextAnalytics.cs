using Newtonsoft.Json.Linq;
using pe.itsight.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace pe.itsight.Sentiment
{
    public class AzureTextAnalytics
    {
        private SentimentResponse _respuesta;
        public AzureTextAnalytics()
        {
            _respuesta = new SentimentResponse();
        }

        //public SentimentResponse GetSentiment(string frase, string id)
        //{            
        //    ITextAnalyticsAPI client = new TextAnalyticsAPI();
        //    client.AzureRegion = AzureRegions.Westus;            
        //    client.SubscriptionKey = ConfigurationManager.AppSettings["AzureCognitiveServices.Key1"];

        //    //Análisis de keywords
        //    KeyPhraseBatchResult result = client.KeyPhrases(
        //        new MultiLanguageBatchInput(
        //            new List<MultiLanguageInput>()
        //            {
        //                new MultiLanguageInput("es", id, frase)
        //            }
        //        )
        //    );
        //    foreach (var document in result.Documents)
        //    {
        //        _respuesta.Id = document.Id;
        //        _respuesta.Keywords.AddRange(document.KeyPhrases);                
        //    }

        //    //Análisis de sentimientos
        //    SentimentBatchResult result2 = client.Sentiment(
        //            new MultiLanguageBatchInput(
        //                new List<MultiLanguageInput>()
        //                {
        //                  new MultiLanguageInput("es", id, frase),                          
        //                }));
        //    var scoreNegativo = double.Parse(ConfigurationManager.AppSettings["AzureCognitiveServices.Negative"]);
        //    var scorePositivo = double.Parse(ConfigurationManager.AppSettings["AzureCognitiveServices.Positive"]);

        //    foreach (var document in result2.Documents)
        //    {
        //        var score = document.Score;
        //        _respuesta.Score = document.Score.HasValue ? document.Score.Value : 0.5;

        //        if (_respuesta.Score < scoreNegativo)
        //            _respuesta.SentimientoId = Sentimiento.Negativo;
        //        else if (_respuesta.Score >= scorePositivo)
        //            _respuesta.SentimientoId = Sentimiento.Positivo;
        //        else
        //            _respuesta.SentimientoId = Sentimiento.Neutral;                
        //    }

        //    return _respuesta;
        //}        

        public SentimentResponse GetSentiment(string frase, string id)
        {
            frase = frase.Replace('"', ' ');

            var scoreNegativo = double.Parse(ConfigurationManager.AppSettings["AzureCognitiveServices.Negative"]);
            var scorePositivo = double.Parse(ConfigurationManager.AppSettings["AzureCognitiveServices.Positive"]);

            var jsonBodyKey = File.ReadAllText(ConfigurationManager.AppSettings["AzureCognitiveServices.InputKeyword"]);
            var jsonBodySentiment = File.ReadAllText(ConfigurationManager.AppSettings["AzureCognitiveServices.InputSentiment"]);

            var url = ConfigurationManager.AppSettings["AzureCognitiveServices.URL"];
            var key = ConfigurationManager.AppSettings["AzureCognitiveServices.Key1"];

            var endpointKey = ConfigurationManager.AppSettings["AzureCognitiveServices.EndpointKey"];
            var endpointSentiment = ConfigurationManager.AppSettings["AzureCognitiveServices.EndpointSentiment"];
            var authorization = ConfigurationManager.AppSettings["AzureCognitiveServices.Header"];
            var contentType = ConfigurationManager.AppSettings["AzureCognitiveServices.ContentType"];

            jsonBodyKey = jsonBodyKey.Replace("[TEXTO]", frase).Replace("[ID", id);
            jsonBodySentiment = jsonBodySentiment.Replace("[TEXTO]", frase).Replace("[ID", id);

            //Análisis de keywords
            RestClient clientKey = new RestClient(new Uri(string.Concat(url, endpointKey)));
            IRestRequest requestKey = new RestRequest(Method.POST);
            requestKey.AddHeader(authorization, key);
            requestKey.AddHeader("Content-type", contentType);
            requestKey.AddParameter("Application/Json", jsonBodyKey, ParameterType.RequestBody);

            _respuesta.Id = id;

            var response = clientKey.Execute(requestKey);
            if (response != null)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = response.Content;
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        //Entradas asociadas a los keywords
                        JObject nluResult = JObject.Parse(result);
                        IList<string> keywords = nluResult.SelectToken("$..keyPhrases").Select(x => (string)x).ToList();
                        _respuesta.Keywords.AddRange(keywords);
                    }
                }
            }

            //Análisis de sentimientos
            RestClient clientSent = new RestClient(new Uri(string.Concat(url, endpointSentiment)));
            IRestRequest requestSent = new RestRequest(Method.POST);
            requestSent.AddHeader(authorization, key);
            requestSent.AddHeader("Content-type", contentType);
            requestSent.AddParameter("Application/Json", jsonBodyKey, ParameterType.RequestBody);

            _respuesta.Id = id;

            response = clientSent.Execute(requestSent);
            if (response != null)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = response.Content;
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        //Entradas asociadas a los keywords
                        JObject nluResult = JObject.Parse(result);
                        double score = (double)nluResult.SelectToken("documents[0].score");
                        _respuesta.Score = score;
                        if (_respuesta.Score < scoreNegativo)
                            _respuesta.SentimientoId = Sentimiento.Negativo;
                        else if (_respuesta.Score >= scorePositivo)
                            _respuesta.SentimientoId = Sentimiento.Positivo;
                        else
                            _respuesta.SentimientoId = Sentimiento.Neutral;
                    }
                    else
                    {
                        _respuesta.Score = 2;
                        _respuesta.SentimientoId = Sentimiento.Error;
                    }
                }
                else
                {
                    _respuesta.Score = 2;
                    _respuesta.SentimientoId = Sentimiento.Error;
                }
            }
            else
            {
                _respuesta.Score = 2;
                _respuesta.SentimientoId = Sentimiento.Error;
            }

            return _respuesta;
        }
    }
}
