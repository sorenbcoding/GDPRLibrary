namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>Default personal-data name lists shared by SB-SEC-003 and SB-SEC-004. Organisations extend them through governance.</summary>
    internal static class PersonalDataNames
    {
        internal const string NamesKey = "personal_data_names";
        internal const string IgnoredKey = "personal_data_ignored_names";

        /// <summary>Words that point to personal data in Danish education workflows, plus a few English ones.</summary>
        internal const string DefaultNames =
            "cpr;cprnr;cprnummer;personnummer;ssn;" +
            "navn;fornavn;efternavn;mellemnavn;elevnavn;surname;" +
            "elev;studerende;kursist;ansøger;" +
            "email;telefon;telefonnummer;mobil;mobilnummer;phone;" +
            "adresse;fødselsdato;foedselsdato;birthdate";

        /// <summary>Compounds that end in a listed word but are not about a person, e.g. Fil + Navn.</summary>
        internal const string DefaultIgnored =
            "filnavn;arknavn;mappenavn;kolonnenavn;tabelnavn;holdnavn;kursusnavn;fagnavn;uddannelsesnavn;" +
            "skolenavn;afdelingsnavn;firmanavn;servernavn;værtsnavn;domænenavn;procesnavn;kønavn;assetnavn";

        internal const string NamesDisplayName = "Names that point to personal data (semicolon-separated). Matched against whole words of identifiers";

        internal const string IgnoredDisplayName = "Names to ignore (semicolon-separated), e.g. filnavn";
    }
}
