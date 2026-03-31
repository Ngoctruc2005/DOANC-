using System;
using System.Collections.Generic;

namespace TourismCMS.Models;

public partial class AdminUser
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public int? RoleId { get; set; }

    public virtual Role? Role { get; set; }
}
