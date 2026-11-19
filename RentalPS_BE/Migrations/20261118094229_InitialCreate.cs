using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentalPSMiniProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsoleTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    additional_hourly_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StickTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    console_type_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    additional_hourly_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickTypes", x => x.id);
                    table.ForeignKey(
                        name: "FK_StickTypes_ConsoleTypes_console_type_id",
                        column: x => x.console_type_id,
                        principalTable: "ConsoleTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_type_id = table.Column<int>(type: "integer", nullable: false),
                    room_number = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status_room = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "RoomTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sticks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    controller_type_id = table.Column<int>(type: "integer", nullable: false),
                    controller_code = table.Column<string>(type: "text", nullable: false),
                    status_stick = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sticks", x => x.id);
                    table.ForeignKey(
                        name: "FK_Sticks_StickTypes_controller_type_id",
                        column: x => x.controller_type_id,
                        principalTable: "StickTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsoleUnits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    console_type_id = table.Column<int>(type: "integer", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: true),
                    unit_code = table.Column<string>(type: "text", nullable: false),
                    status_console = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleUnits", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConsoleUnits_ConsoleTypes_console_type_id",
                        column: x => x.console_type_id,
                        principalTable: "ConsoleTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsoleUnits_Rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "Rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Rentals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rental_code = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    console_unit_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    console_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    status_rental = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals", x => x.id);
                    table.ForeignKey(
                        name: "FK_Rentals_ConsoleUnits_console_unit_id",
                        column: x => x.console_unit_id,
                        principalTable: "ConsoleUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rentals_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rental_id = table.Column<int>(type: "integer", nullable: false),
                    payment_code = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    payment_status = table.Column<string>(type: "text", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Payments_Rentals_rental_id",
                        column: x => x.rental_id,
                        principalTable: "Rentals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalRooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rental_id = table.Column<int>(type: "integer", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    room_type_name = table.Column<string>(type: "text", nullable: false),
                    additional_hourly_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalRooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_RentalRooms_Rentals_rental_id",
                        column: x => x.rental_id,
                        principalTable: "Rentals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalRooms_Rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "Rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalSticks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rental_id = table.Column<int>(type: "integer", nullable: false),
                    controller_id = table.Column<int>(type: "integer", nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalSticks", x => x.id);
                    table.ForeignKey(
                        name: "FK_RentalSticks_Rentals_rental_id",
                        column: x => x.rental_id,
                        principalTable: "Rentals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalSticks_Sticks_controller_id",
                        column: x => x.controller_id,
                        principalTable: "Sticks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleTypes_name",
                table: "ConsoleTypes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleUnits_console_type_id",
                table: "ConsoleUnits",
                column: "console_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleUnits_room_id",
                table: "ConsoleUnits",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleUnits_unit_code",
                table: "ConsoleUnits",
                column: "unit_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_payment_code",
                table: "Payments",
                column: "payment_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_rental_id",
                table: "Payments",
                column: "rental_id");

            migrationBuilder.CreateIndex(
                name: "IX_RentalRooms_rental_id",
                table: "RentalRooms",
                column: "rental_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalRooms_room_id",
                table: "RentalRooms",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_console_unit_id",
                table: "Rentals",
                column: "console_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_rental_code",
                table: "Rentals",
                column: "rental_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_user_id",
                table: "Rentals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_RentalSticks_controller_id",
                table: "RentalSticks",
                column: "controller_id");

            migrationBuilder.CreateIndex(
                name: "IX_RentalSticks_rental_id_controller_id",
                table: "RentalSticks",
                columns: new[] { "rental_id", "controller_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_room_number",
                table: "Rooms",
                column: "room_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_room_type_id",
                table: "Rooms",
                column: "room_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_name",
                table: "RoomTypes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sticks_controller_code",
                table: "Sticks",
                column: "controller_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sticks_controller_type_id",
                table: "Sticks",
                column: "controller_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_StickTypes_console_type_id_name",
                table: "StickTypes",
                columns: new[] { "console_type_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RentalRooms");

            migrationBuilder.DropTable(
                name: "RentalSticks");

            migrationBuilder.DropTable(
                name: "Rentals");

            migrationBuilder.DropTable(
                name: "Sticks");

            migrationBuilder.DropTable(
                name: "ConsoleUnits");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "StickTypes");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "ConsoleTypes");

            migrationBuilder.DropTable(
                name: "RoomTypes");
        }
    }
}
