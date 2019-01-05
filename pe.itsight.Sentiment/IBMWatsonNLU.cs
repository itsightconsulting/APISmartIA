using Newtonsoft.Json.Linq;
using pe.itsight.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.Sentiment
{
    public class IBMWatsonNLU
    {
        private SentimentResponse _respuesta;
        public string JsonBody { get; set; }
        public string Url { get; set; }
        public string Authorization { get; set; }
        public string ContentType { get; set; }

        public IBMWatsonNLU()
        {
            _respuesta = new SentimentResponse();
        }

        public SentimentResponse GetSentiment(string frase, string id)
        {
            frase = frase.Replace('"', ' ');
            var jsonBody = this.JsonBody;
            var url = Url;
            var authorization = Authorization;
            var contentType = ContentType;

            jsonBody = jsonBody.Replace("[TEXTO]", frase);

            RestClient client = new RestClient(new Uri(url));
            IRestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", authorization);
            request.AddHeader("Content-type", contentType);
            request.AddParameter("Application/Json", jsonBody, ParameterType.RequestBody);

            _respuesta.Id = id;

            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.CheckCertificateRevocationList = false;

            var response = client.Execute(request);

            if (response != null)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = response.Content;
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        //Entradas asociadas al análisis de sentimiento                        
                        var jsonRetorno = result;
                        JObject nluResult = JObject.Parse(jsonRetorno);
                        double score = (double)nluResult.SelectToken("sentiment.document.score");
                        string label = (string)nluResult.SelectToken("sentiment.document.label");
                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            if (label == ConfigurationManager.AppSettings["WatsonNLU.Positive"])
                                _respuesta.SentimientoId = Sentimiento.Positivo;
                            else if (label == ConfigurationManager.AppSettings["WatsonNLU.Negative"])
                                _respuesta.SentimientoId = Sentimiento.Negativo;
                            else
                                _respuesta.SentimientoId = Sentimiento.Neutral;
                            _respuesta.Score = score;
                        }
                        else
                        {
                            _respuesta.Score = 2;
                            _respuesta.SentimientoId = Sentimiento.Error;
                        }

                        //Entradas asociadas a los keywords
                        IList<string> keywords = nluResult.SelectToken("keywords").Select(x => (string)x.SelectToken("text")).ToList();
                        _respuesta.Keywords.AddRange(keywords);
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
