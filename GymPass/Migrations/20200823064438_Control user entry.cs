﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GymPass.Migrations
{
    public partial class Controluserentry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TimeAccessRequested",
                table: "Facility",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeAccessRequested",
                table: "Facility");
        }
    }
}
