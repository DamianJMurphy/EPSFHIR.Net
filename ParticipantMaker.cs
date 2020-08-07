using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace EPSFHIR
{
    class ParticipantMaker
    {
        private Practitioner practitioner = null;
        private PractitionerRole role = null;
        private Organization organisation = null;

        public Practitioner Practitioner => practitioner;

        public PractitionerRole Role => role;

        public Organization Organisation => organisation;

        public ParticipantMaker()
        {
        }

        public void Make(int b, List<string> rx)
        {
            practitioner = new Practitioner();
            role = new PractitionerRole();
            organisation = new Organization();
            DoPractitioner(b, rx);
            DoOrg(b, rx);
            DoRole(b, rx);
        }

        private void DoPractitioner(int b, List<string> rx)
        {
            practitioner.Id = FhirHelper.MakeId();
            HumanName h = new HumanName
            {
                Text = rx[b + EMUData.PERSONNAME]
            };
            List<HumanName> ah = new List<HumanName>
            {
                h
            };
            practitioner.Name = ah;
            practitioner.Identifier.Add(FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/sds-user-id", rx[b + EMUData.SDSUSERID]));
        }

        private void DoOrg(int b, List<string> rx)
        {
            organisation.Id = FhirHelper.MakeId();
            organisation.Identifier.Add(FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/ods-organization-code",rx[b + EMUData.SDSORGANISATIONID]));
            ContactPoint cp = new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Use = ContactPoint.ContactPointUse.Work,
                Value = rx[b + EMUData.ORGANISATIONTELECOM]
            };
            organisation.Telecom.Add(cp);
            CodeableConcept cc = new CodeableConcept();
            cc.Coding.Add(FhirHelper.MakeCoding("https://fhir.nhs.uk/R4/CodeSystem/organisation-type", rx[b + EMUData.ORGANISATIONTYPE], null));
            organisation.Type.Add(cc);
            organisation.Name = rx[b + EMUData.ORGANISATIONNAME];
            organisation.Address.Add(MakeAddress(b, rx));
            Identifier pct = FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/ods-organization-code", rx[b + EMUData.PCTORGANISATIONSDSID]);
            ResourceReference pctref = new ResourceReference
            {
                Identifier = pct
            };
            organisation.PartOf = pctref;
        }

        private Address MakeAddress(int b, List<string> rx)
        {
            Address a = new Address();
            AddIfValue(a, rx[b + EMUData.ORGANISATIONADDRESSLINE1]);
            AddIfValue(a, rx[b + EMUData.ORGANISATIONADDRESSLINE2]);
            AddIfValue(a, rx[b + EMUData.ORGANISATIONADDRESSLINE3]);
            AddIfValue(a, rx[b + EMUData.ORGANISATIONADDRESSLINE4]);
            AddIfValue(a, rx[b + EMUData.ORGANISATIONADDRESSLINE5]);
            a.PostalCode = rx[b + EMUData.ORGANISATIONPOSTCODE];
            return a;
        }

        private void AddIfValue(Address a, string s)
        {
            if (s.Trim().Length > 0)
                a.LineElement.Add(new FhirString(s));
        }

        private void DoRole(int b, List<string> rx)
        {
            role.Id = FhirHelper.MakeId();
            role.Identifier.Add(FhirHelper.MakeIdentifier("https://fhir.nhs.uk/Id/sds-role-profile-id", rx[b + EMUData.ROLEPROFILE]));
            role.Practitioner = FhirHelper.MakeInternalReference(practitioner);
            role.Organization = FhirHelper.MakeInternalReference(organisation);
            ContactPoint cp = new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Use = ContactPoint.ContactPointUse.Work,
                Value = rx[b + EMUData.ORGANISATIONTELECOM]
            };
            role.Telecom.Add(cp);
        }

        public bool Has(Practitioner p)
        {
            if (practitioner == null)
                return false;
            if (p == null)
                return false;
            return practitioner.IsExactly(p);
        }

        public bool Has(PractitionerRole r)
        {
            if (role == null)
                return false;
            if (r == null)
                return false;
            return role.IsExactly(r);
        }

        public bool Has(Organization o)
        {
            if (organisation == null)
                return false;
            if (o == null)
                return false;
            return organisation.IsExactly(o);
        }

    }
}
