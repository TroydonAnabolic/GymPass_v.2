using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GymPass.Migrations
{
    public partial class MigratetoAWS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facility",
                columns: table => new
                {
                    FacilityID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityName = table.Column<string>(nullable: true),
                    NumberOfClientsInGym = table.Column<int>(nullable: true),
                    NumberOfClientsUsingWeightRoom = table.Column<int>(nullable: true),
                    NumberOfClientsUsingCardioRoom = table.Column<int>(nullable: true),
                    NumberOfClientsUsingStretchRoom = table.Column<int>(nullable: true),
                    IsOpenDoorRequested = table.Column<bool>(nullable: false),
                    DoorOpened = table.Column<bool>(nullable: false),
                    DoorCloseTimer = table.Column<TimeSpan>(nullable: false),
                    UserTrainingDuration = table.Column<TimeSpan>(nullable: false),
                    TotalTrainingDuration = table.Column<TimeSpan>(nullable: false),
                    WillUseWeightsRoom = table.Column<bool>(nullable: false),
                    WillUseCardioRoom = table.Column<bool>(nullable: false),
                    WillUseStretchRoom = table.Column<bool>(nullable: false),
                    IsCameraScanSuccessful = table.Column<bool>(nullable: false),
                    IsWithin10m = table.Column<bool>(nullable: false),
                    Latitude = table.Column<string>(nullable: true),
                    Longitude = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facility", x => x.FacilityID);
                });

            migrationBuilder.CreateTable(
                name: "ImageStore",
                columns: table => new
                {
                    ImageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageBase64String = table.Column<string>(nullable: true),
                    CreateDate = table.Column<DateTime>(nullable: true),
                    UniqueID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageStore", x => x.ImageId);
                });

            migrationBuilder.CreateTable(
                name: "UsersInGymDetails",
                columns: table => new
                {
                    UsersInGymDetailID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityID = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    TimeAccessGranted = table.Column<DateTime>(nullable: false),
                    EstimatedTrainingTime = table.Column<TimeSpan>(nullable: false),
                    UniqueEntryID = table.Column<string>(nullable: true),
                    IsSmiling = table.Column<bool>(nullable: false),
                    Gender = table.Column<string>(nullable: true),
                    AgeRangeLow = table.Column<int>(nullable: false),
                    AgeRangeHigh = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersInGymDetails", x => x.UsersInGymDetailID);
                    table.ForeignKey(
                        name: "FK_UsersInGymDetails_Facility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "Facility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsersOutofGymDetails",
                columns: table => new
                {
                    UsersOutOfGymDetailsID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityID = table.Column<int>(nullable: false),
                    EstimatedTimeToCheck = table.Column<DateTime>(nullable: false),
                    UniqueEntryID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersOutofGymDetails", x => x.UsersOutOfGymDetailsID);
                    table.ForeignKey(
                        name: "FK_UsersOutofGymDetails_Facility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "Facility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersInGymDetails_FacilityID",
                table: "UsersInGymDetails",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_UsersOutofGymDetails_FacilityID",
                table: "UsersOutofGymDetails",
                column: "FacilityID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageStore");

            migrationBuilder.DropTable(
                name: "UsersInGymDetails");

            migrationBuilder.DropTable(
                name: "UsersOutofGymDetails");

            migrationBuilder.DropTable(
                name: "Facility");
        }
    }
}
