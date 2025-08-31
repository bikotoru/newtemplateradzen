using System;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;

namespace Shared.Models.Entities
{
    [MetadataType(typeof(CategoriaMetadata))]
    public partial class Categoria { }

    public class CategoriaMetadata
    {
        [SoloCrear]
        public string Id;

        [SoloCrear]
        public string Nombre;

        [SoloCrear]
        public string Descripcion;
    }
}
