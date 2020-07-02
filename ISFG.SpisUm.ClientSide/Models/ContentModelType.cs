using ISFG.SpisUm.ClientSide.Attributes;

namespace ISFG.SpisUm.ClientSide.Models
{
    public enum ContentModelType
    {
        [ContentModel("ssl:file")]
        File,
        [ContentModel("ssl:document")]
        Document,
        [ContentModel("ssl:component")]
        Component
    }
}