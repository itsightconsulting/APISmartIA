using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.Entities
{
    public class Analysis
    {
        public ObjectId id { get; set; }
        public string nameKey { get; set; }
        public DateTime startDateAnalysis { get; set; }
        public DateTime? endDateAnalysis { get; set; }
        public int stateAnalysis { get; set; }
        public int originAnalysis { get; set; }
        public string _class { get; set; }

        public List<AnalysisItem> listDetails { get; set; }
        public List<AnalysisHashtag> listHashtags { get; set; }
        public List<UserTwitter> listUser { get; set; }
        public List<AnalysisKeyword> listKeys { get; set; }
        
    }



    public class AnalysisItem
    {
        public ObjectId id { get; set; }

        public ObjectId AnalisisId { get; set; }
         
        public int OrigenAnalisisId { get; set; }

        public long? ObjId { get; set; }

        public string ObjDetalle { get; set; }
        public string ObjDetalleAPI { get; set; }
         
        public bool FlagProcesado { get; set; }

        //Asociados a Twitter
        public int? TweetTotalFavoritos { get; set; }

        public int? TweetTotalReteweet { get; set; }
         
        public string TweetUsuarioId { get; set; }
         
        public string TweetScreenName { get; set; }

        public int? TweetOrigen { get; set; }
         
        public string TweetSource { get; set; }

        public DateTime? TweetFecha { get; set; }

        public string TweetFullText { get; set; }
         
        public string SentimentLabel { get; set; }

        public double? SentimentScore { get; set; }

        public int? SentimentId { get; set; }

        public int? MotorAI { get; set; }

        public int TweetSearchFiltersId { get; set; }

        public string _class { get; set; }
    }
      
    public class AnalysisHashtag
    {
        public ObjectId id { get; set; }

        public ObjectId AnalisisId { get; set; }
        
        public string Hashtag { get; set; }

        public int Total { get; set; }

        public string _class { get; set; }

    }

    public class UserTwitter
    {
        public ObjectId id { get; set; } 
         
        public string TweetUsuarioId { get; set; }
         
        public string TweetScreenName { get; set; }
         
        public string Nombre { get; set; }
         
        public string Descripcion { get; set; }

        public int Sexo { get; set; }

        public string Avatar { get; set; }

        public int TotalSeguidores { get; set; }

        public bool CuentaVerificada { get; set; }

        public string ObjDetalleAPI { get; set; }

        public string _class { get; set; }
    }

    public class AnalysisKeyword
    { 
        public ObjectId id { get; set; }

        public ObjectId AnalisisId { get; set; }
          
        public string Keyword { get; set; }

        public int Total { get; set; }

        public string _class { get; set; }
    }

}
