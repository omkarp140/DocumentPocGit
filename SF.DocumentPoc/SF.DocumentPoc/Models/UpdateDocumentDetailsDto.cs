using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF.DocumentPoc.Models
{
    public class UpdateDocumentDetailsDto
    {
        public Guid? DocumentTypeId { get; set; }
        public DocumentTaggedDto DocumentTaggedDto { get; set; }
    }
}
