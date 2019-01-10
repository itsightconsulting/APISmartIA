using pe.itsight.Entities;
using pe.itsight.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.Sentiment
{
    public class SentimentResponse
    {
        public SentimentResponse()
        {
            Keywords = new List<string>();
        }
        public string Id { get; set; }
        public Sentimiento SentimientoId { get; set; }
        public double Score { get; set; }
        public List<string> Keywords { get; set; }
        public List<AnalysisKeyword> ListKeywords { get; set; }
        public int SentimentId { get; set; }
        public string SentimentLabel { get; set; }
        
    }
}
