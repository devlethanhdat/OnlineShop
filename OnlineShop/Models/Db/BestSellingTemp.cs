using System;
using System.Collections.Generic;

namespace OnlineShop.Models.Db;

public partial class BestSellingTemp
{
    public int ProductId { get; set; }

    public int Count { get; set; }

    public string? Title { get; set; }

    public decimal? Price { get; set; }

    public string? ImageName { get; set; }

    public decimal? Discount { get; set; }

    public string? Status { get; set; }

    public int OrderId { get; set; }
}
