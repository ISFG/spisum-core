namespace ISFG.SpisUm.Jobs.Models
{
    public static class PdfTranslations
    {
        #region Fields

        public static readonly string PageHeader = "Denní otisk TP {0} za den {1}";

        #endregion

        #region Nested Types, Enums, Delegates

        public static class Operations
        {
            #region Fields

            public static readonly string New = "Nová";
            public static readonly string Edit = "Editace";
            public static readonly string Deleted = "Smazání";

            #endregion
        }

        public static class Cells
        {
            #region Fields

            public static readonly string Pid = "PID";
            public static readonly string TypeOfObject = "Typ objektu";
            public static readonly string TypeOfChanges = "Typ změny";
            public static readonly string Descriptions = "Popis změny";
            public static readonly string Author = "Autor změny";
            public static readonly string Date = "Datum změny";

            public static readonly string NewValue = "Nová hodnota:";
            public static readonly string OldValue = "Původní hodnota:";

            public static readonly string OperationTypeUnknown = "Neznamý typ změny";

            #endregion
        }

        public static class FirstPage
        {
            #region Fields

            public static readonly string Name = "Název: Denní otisk TP za den {0}";
            public static readonly string Originator = "Původce: {0}";
            public static readonly string Address = "Adresa: {0}";
            public static readonly string SerialNumber = "Pořadové číslo: {0}";
            public static readonly string NumberOfPages = "Počet listů: ";

            #endregion
        }

        #endregion
    }
}