using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TourismCMS.Data;

namespace TourismApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseBarcodeReader();

        // Cấu hình chuỗi kết nối an toàn (sẽ fallback nếu không tìm thấy key)
        string dbConnection = "Server=.\\SQLEXPRESS;Database=FoodPOI;Trusted_Connection=True;TrustServerCertificate=True";
        if (builder.Configuration != null)
        {
            var confString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(confString)) dbConnection = confString;
        }

        builder.Services.AddDbContext<FoodDbContext>(options =>
            options.UseSqlServer(dbConnection));

        return builder.Build();
    }
}