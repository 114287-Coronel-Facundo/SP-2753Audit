using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Domain.ModelBase
{
    public abstract class AuditableEntity
    {
        [NotMapped]
        public int AuditUserId { get; set; }
    }
}
