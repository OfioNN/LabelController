using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelController.Model {
    internal class ProductLabel {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Ingredients { get; set; } = string.Empty;
        public string? ImagePath { get; set; }

        public string Producer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
