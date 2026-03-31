using System;
using System.Collections.Generic;

namespace TourismCMS.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public virtual ICollection<POI> POIs { get; set; } = new List<POI>();
}
