using System.IO;
using System.Text;

namespace RssFeeder.Mvc.Models
{
    public class StringWriterWithEncoding : StringWriter
    {
        public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
            : base(sb)
        {
            _encoding = encoding;
        }

        private readonly Encoding _encoding;
        public override Encoding Encoding => _encoding;
    }
}
