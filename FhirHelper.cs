using System;
using System.IO;
using System.Text;
using Hl7.Fhir.Model;

namespace EPSFHIR
{
    class FhirHelper
    {
        public static readonly string[] DATEPATTERN = { "yyyyMMdd" };

        public static string MakeId() { return Guid.NewGuid().ToString().ToLower(); }

        public static void Write(string pid, string b, string outputDirectory, bool xml)
        {
            StringBuilder sb = new StringBuilder(outputDirectory);
            if (!outputDirectory.EndsWith("\\"))
                sb.Append("\\");
            sb.Append(pid);
            if (xml)
            {
                sb.Append(".xml");
            }
            else
            {
                sb.Append(".json");
            }
            string fname = sb.ToString();
            using StreamWriter sw = new StreamWriter(fname);
            sw.Write(b);
            sw.Flush();
        }

        public static ResourceReference MakeInternalReference(Resource res)
        {
            if (res.Id == null)
            {
                res.Id = MakeId();
            }
            return new ResourceReference("urn:uuid:" + res.Id);
        }

        public static Identifier MakeIdentifier(string u, string v)
        {
            Identifier id = new Identifier
            {
                System = u,
                Value = v
            };
            return id;
        }

        public static Extension MakeExtension(string u, Element v)
        {
            return MakeExtension(null, u, v);
        }

        public static Extension MakeExtension(Extension e, string u, Element v)
        {
            Extension x = e ?? new Extension();
            x.Url = u;
            x.Value = v;
            return x;
        }

        public static Coding MakeCoding(Coding cd, string s, string c, string d)
        {
            Coding coding = cd ?? new Coding();
            if (s != null)
                coding.System = s;
            if (c != null)
                coding.Code = c;
            if (d != null)
                coding.Display = d;
            return coding;
        }

        public static Coding MakeCoding(string s, string c, string d)
        {
            return MakeCoding(null, s, c, d);
        }

        public static void AddEntryToBundle(Bundle b, Resource r)
        {
            b.AddResourceEntry(r, "urn:uuid:" + r.Id);
        }

        public static bool MakeDate(string s, out DateTime d)
        {
            try
            {
                return DateTime.TryParseExact(s, DATEPATTERN, null, System.Globalization.DateTimeStyles.None, out d);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + ": " + s);
                d = DateTime.UtcNow;
                return true;
            }
        }
    }
}
