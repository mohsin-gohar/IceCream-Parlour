using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ice_Cream_Parlour_Eproject.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            // Price pehle se database mein hai, isliye isko comment kar diya:
            // migrationBuilder.AddColumn<decimal>(
            //     name: "Price",
            //     table: "Recipes",
            //     type: "decimal(18,2)",
            //     nullable: false,
            //     defaultValue: 0m);

            // ProfilePicture add karni hai, isko active kar diya:
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Price ko drop nahi karna kyunki humne upar add nahi kiya
            // migrationBuilder.DropColumn(
            //     name: "Price",
            //     table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedDate", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 30, 16, 14, 3, 257, DateTimeKind.Local).AddTicks(3562), "Traditional ice cream flavors like Vanilla, Chocolate", true, "Classic" },
                    { 2, new DateTime(2026, 6, 30, 16, 14, 3, 257, DateTimeKind.Local).AddTicks(3573), "Fresh fruit based ice creams like Strawberry, Mango", true, "Fruit" },
                    { 3, new DateTime(2026, 6, 30, 16, 14, 3, 257, DateTimeKind.Local).AddTicks(3575), "Luxury and gourmet flavors", true, "Premium" },
                    { 4, new DateTime(2026, 6, 30, 16, 14, 3, 257, DateTimeKind.Local).AddTicks(3577), "Seasonal and limited edition flavors", true, "Special" },
                    { 5, new DateTime(2026, 6, 30, 16, 14, 3, 257, DateTimeKind.Local).AddTicks(3578), "Healthy sugar-free options for diabetics", true, "Sugar Free" }
                });
        }
    }
}