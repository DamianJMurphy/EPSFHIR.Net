using System;
using System.Collections.Generic;
using System.IO;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace EPSFHIR
{
    class MedicationRequestBundleBuilder
    {
        private readonly string prescriptionsFile = null;
        private readonly string itemsFile = null;
        private readonly string outputDirectory = null;

        private static bool xml = false;
        private static string asid = null;
        private static string ods = null;
        private static string url = null;
        private static string resdir = null;

        private static readonly Dictionary<string,string> patients = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> practitioners = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> roles = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> organisations = new Dictionary<string, string>();

        private static string currentNhsNumber = null;
        private static string currentPractitioner = null;
        private static string currentRole = null;
        private static string currentOrganisation = null;

        static void Main(string[] args)
        {
            
            if ((args.Length != 7) && (args.Length != 8)) 
            {
                Console.WriteLine("Arguments: prescriptionsfile itemsfile outputdirectory format asid ods url [resourcedirectory]");
            }
            else
            {
                asid = args[4];
                ods = args[5];
                url = args[6];
                if (args.Length == 8)
                {
                    resdir = args[7];
                }
                try 
                {
                    MedicationRequestBundleBuilder builder = new MedicationRequestBundleBuilder(args[0], args[1], args[2], args[3]);
                    builder.Go();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public MedicationRequestBundleBuilder(string pfile, string ifile, string d, string f)
        {
            prescriptionsFile = pfile;
            itemsFile = ifile;
            outputDirectory = d;
            xml = f.ToLower().StartsWith("x");
        }

        public void Go()
        {
            SanityCheckOutput();
            EMUData emu = new EMUData(prescriptionsFile, itemsFile);
            emu.Load();
            foreach (string pid in emu.GetPrescriptionIDs())
            {
                Bundle b = MakeBundle(emu.GetPrescriptionData(pid), emu.GetItems(pid));
                if (b != null)
                {
                    FhirHelper.WriteResource(pid, b, outputDirectory, xml);
                }
            }
        }

        private static Bundle MakeBundle(System.Collections.Generic.List<string> rx, System.Collections.Generic.List<System.Collections.Generic.List<string>> items)
        {
            Bundle b = new Bundle
            {
                Id = FhirHelper.MakeId(),
                Type = Bundle.BundleType.Message
            };
            ParticipantMaker author = new ParticipantMaker();
            author.Make(EMUData.AUTHORROLEPROFILE, rx);
            GetParticipantIdentifiers(author);
            Patient p = MakePatient(rx);
            if (resdir != null)
            {
                if (!practitioners.ContainsKey(currentPractitioner))
                {
                    FhirHelper.WriteResource(null, author.Practitioner, resdir, xml);
                    practitioners.Add(currentPractitioner, author.Practitioner.Id);
                }
                else
                {
                    author.Practitioner.Id = practitioners[currentPractitioner];
                }
                if (!organisations.ContainsKey(currentOrganisation))
                {
                    FhirHelper.WriteResource(null, author.Organisation, resdir, xml);
                    organisations.Add(currentOrganisation, author.Organisation.Id);
                }
                else
                {
                    author.Organisation.Id = organisations[currentOrganisation];
                }
                if (!roles.ContainsKey(currentRole))
                {
                    FhirHelper.WriteResource(null, author.Role, resdir, xml);
                    roles.Add(currentRole, author.Role.Id);
                }
                else
                {
                    author.Role.Id = roles[currentRole];
                }
                if (!patients.ContainsKey(currentNhsNumber))
                {
                    FhirHelper.WriteResource(null, p, resdir, xml);
                    patients.Add(currentNhsNumber, p.Id);
                }
                else
                {
                    p.Id = patients[currentNhsNumber];
                }
            }
            MessageHeader header = MakeMessageHeader(author);
            FhirHelper.AddEntryToBundle(b, header);
            FhirHelper.AddEntryToBundle(b, p);
            FhirHelper.AddEntryToBundle(b, author.Practitioner);
            FhirHelper.AddEntryToBundle(b, author.Organisation);
            FhirHelper.AddEntryToBundle(b, author.Role);
            ResourceReference nominatedPharmacy = GetNominatedPharmacyReference(rx);

            foreach (System.Collections.Generic.List<string> item in items)
            {
                MedicationRequest m = MakeMedicationRequest(p, rx, item, nominatedPharmacy, author);
                if (m != null)
                {
                    FhirHelper.AddEntryToBundle(b, m);
                    header.Focus.Add(FhirHelper.MakeInternalReference(m));
                    FhirHelper.WriteResource(null, m, resdir, xml);
                }
            }

            header.Focus.Add(FhirHelper.MakeInternalReference(p));
            header.Focus.Add(FhirHelper.MakeInternalReference(author.Role));
            return b;
        }

        private static void GetParticipantIdentifiers(ParticipantMaker p)
        {
            currentOrganisation = p.Practitioner.Identifier[0].Value;
            currentRole = p.Role.Identifier[0].Value;
            currentPractitioner = p.Practitioner.Identifier[0].Value;
        }

        private static MedicationRequest MakeMedicationRequest(Patient p, System.Collections.Generic.List<string> rx,
            System.Collections.Generic.List<string> item, ResourceReference nom, ParticipantMaker a)
        {
            MedicationRequest m = new MedicationRequest
            {
                Id = FhirHelper.MakeId()
            };
            m.Status = MedicationRequest.medicationrequestStatus.Active;
            m.Intent = MedicationRequest.medicationRequestIntent.Order;
            m.Subject = FhirHelper.MakeInternalReference(p);
            m.Identifier.Add(FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/prescription-line-id", item[EMUData.LINEITEMID]));
            ResourceReference rq = FhirHelper.MakeInternalReference(a.Role);
            rq.Display = a.Practitioner.Name[0].Text;
            m.Requester = rq;
            m.AuthoredOn = rx[EMUData.AUTHORPARTICIPATIONTIME];
            m.GroupIdentifier = MakeGroupIdentifier(rx);

            DoPrescriptionType(m, rx);
            DoResponsiblePractitioner(m, a);
            m.Medication = DoMedication(item);
            m.CourseOfTherapyType = MakeCourseOfTherapyType(rx);
            if (item[EMUData.DOSAGEINTRUCTIONS].Trim().Length > 0)
            {
                Dosage di = new Dosage
                {
                    Text = item[EMUData.DOSAGEINTRUCTIONS]
                };
                m.DosageInstruction.Add(di);
                if (item[EMUData.ADDITIONALINSTRUCTIONS].Trim().Length > 0)
                {
                    di.PatientInstruction = item[EMUData.ADDITIONALINSTRUCTIONS];
                }
            }
            m.DispenseRequest = MakeDispenseRequest(nom, rx, item);
            return m;
        }

        private static MedicationRequest.DispenseRequestComponent MakeDispenseRequest(ResourceReference nom, System.Collections.Generic.List<string> rx, 
            System.Collections.Generic.List<string> item)
        {
            MedicationRequest.DispenseRequestComponent dr = new MedicationRequest.DispenseRequestComponent();
            Extension e = FhirHelper.MakeExtension(null, "https://fhir.nhs.uk/R4/StructureDefinition/Extension-performerType",
                FhirHelper.MakeCoding("https://fhir.nhs.uk/R4/CodeSystem/dispensing-site-preference", rx[EMUData.DISPENSINGSITEPREFERENCE], null));
            try
            {
                dr.Quantity = new SimpleQuantity
                {
                    Code = item[EMUData.QUANTITYCODE],
                    System = "http://snomed.info/sct",
                    Unit = item[EMUData.QUANTITYTEXT],
                    Value = Convert.ToDecimal(item[EMUData.QUANTITYCOUNT])
                };
            }
            catch (Exception ex)
            {
#pragma warning disable CA2241 // Provide correct arguments to formatting methods
                Console.WriteLine("Exception: {ex} making SimpleQuantity: {value}", ex.Message, item[EMUData.QUANTITYCOUNT]);
#pragma warning restore CA2241 // Provide correct arguments to formatting methods
            }
            if (nom != null)
            {
                dr.Performer = nom;
            }
            dr.Extension.Add(e);
            return dr;
        }
            

        private static CodeableConcept MakeCourseOfTherapyType(System.Collections.Generic.List<string> rx)
        {
            CodeableConcept cc = new CodeableConcept();
            Coding c = new Coding
            {
                System = "https://fhir.nhs.uk/R4/CodeSystem/UKCore-PrescriptionType"
            };
            cc.Coding.Add(c);
            try
            {
                string t = rx[EMUData.PRESCRIPTIONTREATMENTTYPE];
                if ((t.Length == 0) || (t.Equals("0001"))) {
                    c.Code = "acute";
                    c.Display = "Acute";
                }
                else
                {
                    if (t.Equals("0002"))
                    {
                        c.Code = "repeat";
                        c.Display = "Repeat";
                    }
                    else
                    {
                        c.Code = "repeat-dispensing";
                        c.Display = "Repeat Dispensing";
                    }
                }
            }
            catch (Exception)
            {
                c.Code = "acute";
                c.Display = "Acute";
            }
            return cc;
        }

        private static CodeableConcept DoMedication(System.Collections.Generic.List<string> item)
        {
            CodeableConcept cc = new CodeableConcept();
            cc.Coding.Add(FhirHelper.MakeCoding("http://snomed.info/sct", item[EMUData.SUBSTANCECODE], item[EMUData.DISPLAYNAME]));
            return cc;
        }

        private static Identifier MakeGroupIdentifier(System.Collections.Generic.List<string> rx)
        {
            Identifier sfid = FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/prescription-short-form",
                    rx[EMUData.PRESCRIPTIONID]);
            Extension e = FhirHelper.MakeExtension(null, "https://fhir.nhs.uk/R4/StructureDefinition/Extension-PrescriptionId",
                FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/prescription", rx[EMUData.PRESCRIPTIONCLINICALSTATEMENTID]));
            sfid.Extension.Add(e);
            return sfid;
        }

        private static void DoPrescriptionType(MedicationRequest m,  System.Collections.Generic.List<string> rx)
        {
            Coding ptc = FhirHelper.MakeCoding("https://fhir.nhs.uk/R4/CodeSystem/prescription-type", rx[EMUData.PRESCRIPTIONTYPE], null);
            if (rx[EMUData.PRESCRIPTIONTYPE].Equals("0001"))
            {
                ptc.Display = "General Practitioner Prescribing";
            }
            Extension e = FhirHelper.MakeExtension(null, "https://fhir.nhs.uk/R4/StructureDefinition/Extension-prescriptionType", ptc);
            m.Extension.Add(e);
        }

        private static void DoResponsiblePractitioner(MedicationRequest m, ParticipantMaker a)
        {
            Extension e = FhirHelper.MakeExtension(null, "https://fhir.nhs.uk/R4/StructureDefinition/Extension-DM-ResponsiblePractitioner",
                FhirHelper.MakeInternalReference(a.Role));
            m.Extension.Add(e);
        }

        private static ResourceReference GetNominatedPharmacyReference(System.Collections.Generic.List<string> rx)
        {
            ResourceReference r = null;
            string n = rx[EMUData.NOMINATEDPHARMACYID].Trim();
            if (n.Length > 0)
            {
                r = new ResourceReference
                {
                    Identifier = FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/ods-organization-code", n)
                };
            }
            return r;
        }

        private static void AddPatientName(Patient p, System.Collections.Generic.List<string> rx)
        {
            HumanName n = new HumanName
            {
                Use = HumanName.NameUse.Official,
                Family = rx[EMUData.PATIENTSURNAME]
            };
            if (rx[EMUData.PATIENTNAMETITLE].Trim().Length > 0)
            {
                n.PrefixElement.Add(new FhirString(rx[EMUData.PATIENTNAMETITLE]));
            }
            n.GivenElement.Add(new FhirString(rx[EMUData.PATIENTGIVENNAME1]));
            if (rx[EMUData.PATIENTGIVENNAME2].Trim().Length > 0)
            {
                n.PrefixElement.Add(new FhirString(rx[EMUData.PATIENTGIVENNAME2]));
            }
            p.Name.Add(n);
        }

        private static void AddNhsNumber(Patient p, System.Collections.Generic.List<string> rx)
        {
            currentNhsNumber = rx[EMUData.PATIENTID];
            Identifier n = FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/nhs-number", rx[EMUData.PATIENTID]);
            Extension evs = new Extension
            {
                Url = "https://fhir.nhs.uk/R4/StructureDefinition/Extension-UKCore-NHSNumberVerificationStatus"
            };
            CodeableConcept vccvs = new CodeableConcept();
            vccvs.Coding.Add(FhirHelper.MakeCoding("https://fhir.nhs.uk/R4/CodeSystem/UKCore-NHSNumberVerificationStatus",
                    "01", "Number present and verified"));
            evs.Value = vccvs;
            n.Extension.Add(evs);
            p.Identifier.Add(n);
        }

        private static Patient MakePatient(System.Collections.Generic.List<string> rx)
        {
            DateTime d = new DateTime(); 
            if (!FhirHelper.MakeDate(rx[EMUData.PATIENTBIRTHTIME], out d))
            {
                Console.WriteLine("Failed to parse birth date: " + rx[EMUData.PATIENTBIRTHTIME]);
            }
            Patient p = new Patient
            {
                Id = FhirHelper.MakeId(),
                BirthDate = FhirHelper.FormatDate(d),
                Gender = GetGender(rx[EMUData.PATIENTGENDER])
            };
            AddNhsNumber(p, rx);
            AddPatientName(p, rx);
            AddPatientAddress(p, rx);
            AddPatientGp(p, rx);
            return p;
        }

        private static void AddIfPresent(Address a, System.Collections.Generic.List<string> rx, int offset)
        {
                String s = rx[offset].Trim();
                if (s.Length > 0)
                {
                    a.LineElement.Add(new FhirString(s));
                }
        }


        private static void AddPatientAddress(Patient p, System.Collections.Generic.List<string> rx)
        {
            Address a = new Address
            {
                Use = Address.AddressUse.Home
            };
            AddIfPresent(a, rx, EMUData.PATIENTADDRESSLINE1);
            AddIfPresent(a, rx, EMUData.PATIENTADDRESSLINE2);
            AddIfPresent(a, rx, EMUData.PATIENTADDRESSLINE3);
            AddIfPresent(a, rx, EMUData.PATIENTADDRESSLINE4);
            AddIfPresent(a, rx, EMUData.PATIENTADDRESSLINE5);
            a.PostalCode = rx[EMUData.PATIENTADDRESSPOSTCODE];
            p.Address.Add(a);
        }

        private static void AddPatientGp(Patient p, System.Collections.Generic.List<string> rx)
        {
            ResourceReference r = new ResourceReference
            {
                Identifier = FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/ods-organization-code", rx[EMUData.PATIENTPRIMARYCAREPROVIDESDSID])
            };
            p.ManagingOrganization = r;
        }

        private static  AdministrativeGender GetGender(string s)
        {
            try
            {
                int g = Convert.ToInt32(s);
                switch (g)
                {
                    case 0:
                        return AdministrativeGender.Unknown;
                    case 1:
                        return AdministrativeGender.Male;
                    case 2:
                        return AdministrativeGender.Female;
                }
            }
            catch (Exception)
            {
                return AdministrativeGender.Unknown;
            }
            return AdministrativeGender.Other;
        }

        private static MessageHeader MakeMessageHeader(ParticipantMaker a)
        {
            MessageHeader header = new MessageHeader
            {
                Id = FhirHelper.MakeId(),
                Event = FhirHelper.MakeCoding("https://fhir.nhs.uk/R4/CodeSystem/message-event", "prescription-order", "Prescription Order"),
                Sender = FhirHelper.MakeInternalReference(a.Role)
            };
            header.Sender.Display = a.Practitioner.Name[0].Text;
            header.Source = MakeSource();
            return header;
        }

        private static MessageHeader.MessageSourceComponent MakeSource()
        {
            MessageHeader.MessageSourceComponent s = new MessageHeader.MessageSourceComponent();
            Extension a = FhirHelper.MakeExtension("https://fhir.nhs.uk/R4/StructureDefinition/Extension-spineEndpoint",
                                FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/spine-ASID", asid));
            s.Extension.Add(a);
            s.Name = ods;
            s.Endpoint = url;
            return s;
        }

        private void SanityCheckOutput()
        {
            if (outputDirectory == null)
            {
                throw new Exception("Undefined output directory");
            }
            if (!Directory.Exists(outputDirectory))
            {
                throw new Exception("Output directory " + outputDirectory + " does not exist");
            }
        }
    }
}
