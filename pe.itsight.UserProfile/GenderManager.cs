using Newtonsoft.Json.Linq;
using pe.itsight.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.UserProfile
{
    public class GenderManager
    {
        public GenderManager()
        {

        }

        public int GetGender(string name)
        {
            var url = ConfigurationManager.AppSettings["GenderAPI.URL"];
            url = string.Format(url, name, "&key");

            RestSharp.RestClient client = new RestSharp.RestClient(new Uri(url));
            RestSharp.IRestRequest request = new RestSharp.RestRequest(RestSharp.Method.GET);

            var validarSSL = bool.Parse(ConfigurationManager.AppSettings["GenderAPI.SSLDeshabilitar"]);
            if (validarSSL)
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPoliciyErrors) => true;

            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            var response = client.Execute(request);
            if (response != null)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonRetorno = response.Content;
                    JObject genderResult = JObject.Parse(jsonRetorno);
                    string genero = (string)genderResult.SelectToken("gender");
                    if (!string.IsNullOrWhiteSpace(genero))
                    {
                        if (genero == ConfigurationManager.AppSettings["GenderAPI.Masculino"])
                            return (int)Sexo.Masculino;
                        else if (genero == ConfigurationManager.AppSettings["GenderAPI.Femenino"])
                            return (int)Sexo.Femenino;
                        else
                            return (int)Sexo.SinDeterminar;
                    }
                    else
                        return (int)Sexo.SinDeterminar;
                }
                else
                {
                    return (int)Sexo.NoProcesado;
                }
            }
            else
            {
                return (int)Sexo.SinDeterminar;
            }
        }
    }
}
