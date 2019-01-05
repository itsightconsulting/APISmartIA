using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.Util
{
 
        public enum EstadoAnalisis : int
        {
            Registrado = 1,
            DatosRecolectados = 2,
            AnalisisSemantico = 3,
            Completado = 4
        }
        public enum OrigenAnalisis : int
        {
            Twitter = 1,
            Instagram = 2,
            Facebook = 3,
            Google = 4,
            Otros = 5
        }

        public enum OrigenTweet : int
        {
            Retweet = 1,
            OriginalPost = 2,
            Replies = 3
        }

        public enum EntityIds : int
        {
            Analisis = 1,
            UsuarioTwitter = 2,
            Dashboard = 3
        }

        public enum Sexo : int
        {
            Masculino = 1,
            Femenino = 2,
            SinDeterminar = 3,
            NoProcesado = 4
        }

        public enum Sentimiento : int
        {
            Positivo = 1,
            Negativo = -1,
            Neutral = 0,
            Error = -2
        }

        public enum MotorCloudAI : int
        {
            IBMWatson = 1,
            AzureCognitive = 2
        }
 

}
