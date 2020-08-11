namespace ISFG.Pdf.Models
{
    public class PdfNames
    {
        #region Nested Types, Enums, Delegates

        public static class ShreddingPlan
        {
            #region Fields

            public static readonly string FileMark = "Spisový znak";
            public static readonly string DocumentType = "Druh dokumentu";
            public static readonly string ShreddingMode = "Skartační režim";

            #endregion
        }

        public static class Clause
        {
            #region Fields

            public const string Sign = "Dokument vstupující do změny datového formátu byl podepsán zaručeným elektronickým podpisem založeným na kvalifikovaném certifikátu vydaném akreditovaným poskytovatelem certifikačních služeb a platnost zaručeného elektronického podpisu byla ověřena dne {0}. " +
                                       "Zaručený elektronický podpis byl shledán platným, dokument nebyl změněn a ověření platnosti kvalifikovaného certifikátu bylo provedeno vůči seznamu zneplatněných kvalifikovaných certifikátů vydanému k datu {1}. Údaje o zaručeném elektronickém podpisu: číslo kvalifikovaného certifikátu {2}, kvalifikovaný certifikát byl vydán akreditovaným poskytovatelem certifikačních služeb {3}, pro podepisující osobu {4}.";

            public const string Seal = "Dokument vstupující do změny datového formátu byl opatřen elektronickou pečetí založenou na kvalifikovaném certifikátu vydaném akreditovaným poskytovatelem certifikačních služeb a platnost elektronické pečetě byla ověřena dne {0}. " +
                                       "Elektronická pečeť byla shledána platnou, dokument nebyl změněn a ověření platnosti kvalifikovaného certifikátu bylo provedeno vůči seznamu zneplatněných kvalifikovaných certifikátů vydanému k datu {1}. Údaje o zaručeném elektronické pečeti: číslo kvalifikovaného certifikátu {2}, kvalifikovaný certifikát byl vydán akreditovaným poskytovatelem certifikačních služeb {3}, pro podepisující právnickou osobu {4}.";

            public const string TimeStamp = "Časové razítko bylo shledáno platným, dokument nebyl změněn a ověření platnosti kvalifikovaného certifikátu bylo provedeno vůči seznamu zneplatněných kvalifikovaných certifikátů vydanému k datu {0}. Údaje o časovém razítku:datum a čas {1}, číslo kvalifikovaného časového razítka {2}, kvalifikované časové razítko bylo vydáno akreditovaným poskytovatelem certifikačních služeb {3}.";

            public const string Paragraph1 = "Ověřovací doložka změny datového formátu dokumentu v digitální podobě podle § 69a zákona č. 499/2004 Sb.";
            public const string Paragraph2 = "Touto doložkou není potvrzena správnost ani pravdivost údajů obsažených v dokumentu a jejich soulad s právními předpisy.";

            public static readonly string AdvancedSignature = "ADVANCED_SIGNATURE";
            public static readonly string AcreditedSignature = "ACREDITED_SIGNATURE";
            public static readonly string QualifiedSginature = "QUALIFIED_SIGNATURE";
            public static readonly string AdvancedSeal = "ADVANCED_SEAL";
            public static readonly string TrustedSeal = "TRUSTED_SEAL";
            public static readonly string AcreditedSeal = "ACREDITED_SEAL";
            public static readonly string QualifiedSeal = "QUALIFIED_SEAL";
            public static readonly string Timestamp = "TIMESTAMP";
            public static readonly string QualifiedTimestamp = "QUALIFIED_TIMESTAMP";

            public static readonly string OriginalFileFormat = "Typ vstupního dokumentu:";
            public static readonly string FilePrint = "Otisk souboru:";
            public static readonly string UsedAlgorithm = "Použitý algoritmus:";
            public static readonly string Organizer = "Subjekt provádějící změnu datového formátu:";
            public static readonly string NameLastName = "Jméno a příjmení autora změny datového formátu:";
            public static readonly string DateOfIssue = "Datum vyhotovení ověřovací doložky:";

            #endregion
        }

        #endregion
    }
}