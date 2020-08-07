using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EPSFHIR
{
    class EMUData
    {
        private readonly string prescriptionFile = null;
        private readonly string itemFile = null;
        private static readonly char[] tab = { '\t' };

        private readonly Dictionary<string, string> utfSubstitutions = new Dictionary<string, string>();

        private readonly Dictionary<string, List<string>> prescriptions = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<List<string>>> items = new Dictionary<string, List<List<string>>>();

        public EMUData(string pfile, string ifile)
        {
            prescriptionFile = pfile;
            itemFile = ifile;
            for (int i = 0; i < subs.Length; i += 2)
            {
                utfSubstitutions.Add(subs[i], subs[i + 1]);
            }
        }

        public List<string> GetPrescriptionData(string id) { return prescriptions[id];  }
        public List<List<string>> GetItems(string id) { return items[id];  }
        public Dictionary<string, List<string>>.KeyCollection GetPrescriptionIDs() { return prescriptions.Keys; }

        public void Load()
        {
            LoadPrescriptions();
            LoadItems();
        }

        private void LoadPrescriptions()
        {
            using StreamReader sr = new StreamReader(prescriptionFile);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                List<string> l = ReadLine(line);
                prescriptions[l[ID]] = l;
            }
        }

        private void LoadItems()
        {
            using StreamReader sr = new StreamReader(itemFile);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                List<string> iline = ReadLine(line);

                List<List<string>> iset;
                if (items.ContainsKey(iline[ID]))
                {
                    iset = items[iline[ID]];
                }
                else
                {
                    iset = new List<List<string>>();
                    items[iline[ID]] = iset;
                }
                iset.Add(iline);
            }
        }

        private List<string> ReadLine(string l)
        {
            List<string> line = new List<string>();
            string s = DoSubsitutions(l);
            string[] f = s.Split(tab, StringSplitOptions.None);
            foreach (string field in f)
            {
                line.Add(field);
            }
            return line;
        }

        private string DoSubsitutions(string s)
        {
            foreach (string u in utfSubstitutions.Keys)
            {
                if (s.Contains(u))
                    s = s.Replace(u, utfSubstitutions[u]);

            }
            return s;
        }

        private static readonly string[] subs = {"__COPYRIGHT__", "\u00a9", "__REGISTERED__", "\u00ae", "__YEN__", "\u00a5", "__POUND__", "\u00a3", "__DEGREE__", "\u00b0",
            "__aGRAVE__", "\u00e0", "__eGRAVE__", "\u00e8", "__iGRAVE__", "\u00ec", "__oGRAVE__", "\u00f2", "__uGRAVE__", "\u00f9", "__AGRAVE__", "\u00c0",
            "__EGRAVE__", "\u00c8", "__IGRAVE__", "\u00cc", "__OGRAVE__", "\u00d2", "__UGRAVE__", "\u00d9", "__aCIRCUMFLEX__", "\u00e2", "__eCIRCUMFLEX__", "\u00ea",
            "__iCIRCUMFLEX__", "\u00ee", "__oCIRCUMFLEX__", "\u00f4", "__uCIRCUMFLEX__", "\u00fb", "__ACIRCUMFLEX__", "\u00c2", "__ECIRCUMFLEX__", "\u00ca",
            "__ICIRCUMFLEX__", "\u00ce", "__OCIRCUMFLEX__", "\u00d4", "__UCIRCUMFLEX__", "\u00db", "__LATINaDIAERESIS__", "\u00e4", "__eDIAERESIS__", "\u00eb",
            "__iDIAERESIS__", "\u00ef", "__oDIAERESIS__", "\u00f6", "__uDIAERESIS__", "\u00fc", "__yDIAERESIS__", "\u00ff", "__ADIAERESIS__", "\u00c4",
            "__EDIAERESIS__", "\u00cb", "__ODIAERESIS__", "\u00d6", "__UDIAERESIS__", "\u00dc", "__YDIAERESIS__", "\u0178", "__aTILDE__", "\u00e3", "__oTILDE__", "\u00f5",
            "__nTILDE__", "\u00f1", "__aRING__", "\u00e5", "__oSTROKE__", "\u00f8", "__sCARON__", "\u0161", "__ATILDE__", "\u00c3", "__OTILDE__", "\u00d5",
            "__NTILDE__", "\u00d1", "__ARING__", "\u00c5", "__OSTROKE__", "\u00d8", "__cCEDILLA__", "\u00e7", "__CCEDILLA__", "\u00c7", "__DIAERESIS__", "\u00a8",
            "__TRADEMARK__", "\u2122", "__EURO__", "\u20ac", "__CENT__", "\u00a2", "__CURRENCY__", "\u00a4", "__GAMMA__", "\u0194", "__RH_SINGLE__", "\u2019",
            "__LH_SINGLE__", "\u2018", "__LINE_FEED__", "\u000a", "__DASH__", "\u2013", "__CURVED_APOSTROPHE__", "\u055a"
        };


        // Prescriptions
        public const int ID = 0;
        public const int URGENT = 1;
        public const int PATIENTID = 2;
        public const int PATIENTADDRESSLINE1 = 3;
        public const int PATIENTADDRESSLINE2 = 4;
        public const int PATIENTADDRESSLINE3 = 5;
        public const int PATIENTADDRESSLINE4 = 6;
        public const int PATIENTADDRESSLINE5 = 7;
        public const int PATIENTADDRESSPOSTCODE = 8;
        public const int PATIENTADDRESSPAFCODE = 9;
        public const int PATIENTADDRESSTYPE = 10;
        public const int PATIENTADDRESSUSEFROM = 11;
        public const int PATIENTADDRESSUSETO = 12;
        public const int PATIENTNAMETITLE = 13;
        public const int PATIENTGIVENNAME1 = 14;
        public const int PATIENTGIVENNAME2 = 15;
        public const int PATIENTSURNAME = 16;
        public const int PATIENTNAMESUFFIX = 17;
        public const int PATIENTNAMETYPE = 18;
        public const int PATIENTNAMEUSEFROM = 19;
        public const int PATIENTNAMEUSETO = 20;
        public const int PATIENTGENDER = 21;
        public const int PATIENTBIRTHTIME = 22;
        public const int PATIENTDECEASEDTIME = 23;
        public const int PATIENTPRIMARYCAREPROVIDESDSID = 24;
        public const int REPEATNUMBER = 25;
        public const int MAXREPEATPRESCRIPTIONS = 26;
        public const int MAXIMUMREPEATDISPENSES = 27;
        public const int DAYSSUPPLYFROM = 28;
        public const int DAYSSUPPLYTO = 29;
        public const int EXPECTEDUSE = 30;
        public const int TOKENISSUED = 31;
        public const int PRESCRIPTIONTREATMENTTYPE = 32;
        public const int PRESCRIPTIONTYPE = 33;
        public const int TEMPORARYEXEMPTIONINFORMATION = 34;
        public const int TEMPORARYEXEMPTIONFROM = 35;
        public const int TEMPORARYEXEMPTIONTO = 36;
        public const int REVIEWDATE = 37;
        public const int ORIGINALPRESCRPTIONREFERENCE = 38;
        public const int NOMINATEDPHARMACYID = 39;
        public const int DISPENSINGSITEPREFERENCE = 40;
        public const int AUTHORROLEPROFILE = 41;
        public const int AUTHORJOBCODE = 42;
        public const int AUTHORSDSUSERID = 43;
        public const int AUTHORPERSONNAME = 44;
        public const int AUTHORPERSONTELECOM = 45;
        public const int AUTHORSDSORGANISATIONID = 46;
        public const int AUTHORORGANISATIONNAME = 47;
        public const int AUTHORORGANISATIONTYPE = 48;
        public const int AUTHORORGANISATIONTELECOM = 49;
        public const int AUTHORORGANISATIONADDRESSLINE1 = 50;
        public const int AUTHORORGANISATIONADDRESSLINE2 = 51;
        public const int AUTHORORGANISATIONADDRESSLINE3 = 52;
        public const int AUTHORORGANISATIONADDRESSLINE4 = 53;
        public const int AUTHORORGANISATIONADDRESSLINE5 = 54;
        public const int AUTHORORGANISATIONPOSTCODE = 55;
        public const int AUTHORPCTORGANISATIONSDSID = 56;
        public const int AUTHORPARTICIPATIONTIME = 57;
        public const int RESPONSIBLEPARTYROLEPROFILE = 58;
        public const int RESPONSIBLEPARTYJOBCODE = 59;
        public const int RESPONSIBLEPARTYSDSUSERID = 60;
        public const int RESPONSIBLEPARTYPERSONNAME = 61;
        public const int RESPONSIBLEPARTYPERSONTELECOM = 62;
        public const int RESPONSIBLEPARTYSDSORGANISATIONID = 63;
        public const int RESPONSIBLEPARTYORGANISATIONNAME = 64;
        public const int RESPONSIBLEPARTYORGANISATIONTYPE = 65;
        public const int RESPPARTYORGANISATIONTELECOM = 66;
        public const int RESPPARTYORGANISATIONADDRESSLINE1 = 67;
        public const int RESPPARTYORGANISATIONADDRESSLINE2 = 68;
        public const int RESPPARTYORGANISATIONADDRESSLINE3 = 69;
        public const int RESPPARTYORGANISATIONADDRESSLINE4 = 70;
        public const int RESPPARTYORGANISATIONADDRESSLINE5 = 71;
        public const int RESPPARTYORGANISATIONPOSTCODE = 72;
        public const int RESPPARTYPCTORGANISATIONSDSID = 73;
        public const int RESPONSIBLEPARTYPARTICIPATIONTIME = 74;
        public const int LEGALAUTHENTICATORROLEPROFILE = 75;
        public const int LEGALAUTHENTICATORJOBCODE = 76;
        public const int LEGALAUTHENTICATORSDSUSERID = 77;
        public const int LEGALAUTHENTICATORPERSONNAME = 78;
        public const int LEGALAUTHENTICATORPERSONTELECOM = 79;
        public const int LEGALAUTHENTICATORSDSORGANISATIONID = 80;
        public const int LEGALAUTHENTICATORORGANISATIONNAME = 81;
        public const int LEGALAUTHENTICATORORGANISATIONTYPE = 82;
        public const int LEGALAUTHORGANISATIONTELECOM = 83;
        public const int LEGALAUTHORGANISATIONADDRESSLINE1 = 84;
        public const int LEGALAUTHORGANISATIONADDRESSLINE2 = 85;
        public const int LEGALAUTHORGANISATIONADDRESSLINE3 = 86;
        public const int LEGALAUTHORGANISATIONADDRESSLINE4 = 87;
        public const int LEGALAUTHORGANISATIONADDRESSLINE5 = 88;
        public const int LEGALAUTHORGANISATIONPOSTCODE = 89;
        public const int LEGALAUTHPCTORGANISATIONSDSID = 90;
        public const int LEGALAUTHENTICATORPARTICIPATIONTIME = 91;
        public const int HANDLING = 92;
        public const int PRESCRIPTIONCLINICALSTATEMENTID = 93;

        // Line items
        public const int PRESCRIPTIONID = 0;
        public const int SUBSTANCECODE = 1;
        public const int DISPLAYNAME = 2;
        public const int ORIGINALTEXT = 3;
        public const int QUANTITYTEXT = 4;
        public const int QUANTITYCODE = 5;
        public const int QUANTITYCOUNT = 6;
        public const int DOSAGEINTRUCTIONS = 7;
        public const int ADDITIONALINSTRUCTIONS = 8;
        public const int ORIGINALITEMREF = 9;
        public const int PRESCRIBERENDORSEMENT = 10;
        public const int INTENDEDMEDICATIONREF = 11;
        public const int INTENDEDMEDICATIONMOOD = 12;
        public const int DOSEQUANTITY = 13;
        public const int RATEQUANTITY = 14;
        public const int ITEMREPEATNUMBER = 15;
        public const int MAXREPEATS = 16;
        public const int LINEITEMID = 17;

        // Participant offsets
        public const int ROLEPROFILE = 0;
        public const int JOBCODE = 1;
        public const int SDSUSERID = 2;
        public const int PERSONNAME = 3;
        public const int PERSONTELECOM = 4;
        public const int SDSORGANISATIONID = 5;
        public const int ORGANISATIONNAME = 6;
        public const int ORGANISATIONTYPE = 7;
        public const int ORGANISATIONTELECOM = 8;
        public const int ORGANISATIONADDRESSLINE1 = 9;
        public const int ORGANISATIONADDRESSLINE2 = 10;
        public const int ORGANISATIONADDRESSLINE3 = 11;
        public const int ORGANISATIONADDRESSLINE4 = 12;
        public const int ORGANISATIONADDRESSLINE5 = 13;
        public const int ORGANISATIONPOSTCODE = 14;
        public const int PCTORGANISATIONSDSID = 15;
        public const int PARTICIPATIONTIME = 16;

    }
}
