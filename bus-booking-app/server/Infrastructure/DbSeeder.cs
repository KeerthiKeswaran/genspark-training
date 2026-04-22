using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Core.Enums;
using server.Infrastructure.Data;
using System.Linq;

namespace server.Infrastructure
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            Console.WriteLine("--- Database Seeding Started ---");
            
            // 1. Seed Operators (Ensuring at least 10 with full metadata)
            Console.WriteLine("Checking/Seeding operators...");
            var operatorPool = new List<BusOperator>
            {
                new BusOperator { FullName = "IntrCity SmartBus", CompanyName = "IntrCity", Email = "contact@intrcity.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Hubli, Karnataka" },
                new BusOperator { FullName = "RP Tours and Travels", CompanyName = "RP Tours", Email = "info@rptours.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Chennai, Tamil Nadu" },
                new BusOperator { FullName = "VRL Travels", CompanyName = "VRL", Email = "support@vrl.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Belagavi, Karnataka" },
                new BusOperator { FullName = "ZingBus", CompanyName = "ZingBus", Email = "hello@zingbus.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Gurugram, Haryana" },
                new BusOperator { FullName = "Parveen Travels", CompanyName = "Parveen", Email = "booking@parveen.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Koyambedu, Chennai" },
                new BusOperator { FullName = "SRS Travels", CompanyName = "SRS", Email = "info@srstravels.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Kalamboli, Mumbai" },
                new BusOperator { FullName = "KPN Travels", CompanyName = "KPN", Email = "care@kpn.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Salem, Tamil Nadu" },
                new BusOperator { FullName = "National Travels", CompanyName = "National", Email = "admin@national.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Bangalore, Karnataka" },
                new BusOperator { FullName = "Orange Tours", CompanyName = "Orange", Email = "help@orange.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Hyderabad, Telangana" },
                new BusOperator { FullName = "Greenline Travels", CompanyName = "Greenline", Email = "support@greenline.com", PasswordHash = "dummy", Role = UserRole.Operator, IsApproved = true, Address = "Pune, Maharashtra" }
            };

            foreach (var op in operatorPool)
            {
                var existing = await context.BusOperators.FirstOrDefaultAsync(u => u.Email == op.Email);
                if (existing == null)
                {
                    context.BusOperators.Add(op);
                }
                else
                {
                    // Update existing to ensure CompanyName and Address are populated
                    existing.CompanyName = op.CompanyName;
                    existing.Address = op.Address;
                    existing.IsApproved = true;
                }
            }
            await context.SaveChangesAsync();

            // 2. Seed Routes
            Console.WriteLine("Seeding routes...");
            var cities = new[] { 
                "Delhi", "Mumbai", "Bangalore", "Chennai", "Hyderabad", 
                "Pune", "Ahmedabad", "Kolkata", "Lucknow", "Jaipur", 
                "Coimbatore", "Madurai", "Kochi", "Visakhapatnam", "Patna" 
            };

            var hubs = new[] { "Delhi", "Mumbai", "Bangalore", "Hyderabad", "Chennai" };
            
            foreach (var hub in hubs)
            {
                foreach (var city in cities)
                {
                    if (hub == city) continue;
                    if (!await context.Routes.AnyAsync(r => r.Source == hub && r.Destination == city))
                    {
                        context.Routes.Add(new BusRoute { Source = hub, Destination = city, DistanceKm = 400 });
                    }
                    if (!await context.Routes.AnyAsync(r => r.Source == city && r.Destination == hub))
                    {
                        context.Routes.Add(new BusRoute { Source = city, Destination = hub, DistanceKm = 400 });
                    }
                }
            }
            await context.SaveChangesAsync();

            // 3. Seed Buses and Schedules
            Console.WriteLine("Seeding buses and schedules (this may take a minute)...");
            var routes = await context.Routes.ToListAsync();
            var operators = await context.BusOperators.ToListAsync();
            var random = new Random();

            int routesProcessed = 0;
            foreach (var route in routes)
            {
                routesProcessed++;
                if (routesProcessed % 20 == 0) Console.WriteLine($"Processed {routesProcessed}/{routes.Count} routes...");

                int currentBusCount = await context.Buses.CountAsync(b => b.AssignedRouteId == route.Id);
                
                if (currentBusCount < 5)
                {
                    int busesToCreate = 5 - currentBusCount;
                    for (int i = 0; i < busesToCreate; i++)
                    {
                        var op = operators[random.Next(operators.Count)];
                        string prefix = string.IsNullOrEmpty(op.CompanyName) || op.CompanyName.Length < 2 
                            ? "BS" 
                            : op.CompanyName.Substring(0, 2).ToUpper();

                        var bus = new Bus { 
                            BusNumber = $"{prefix}-{random.Next(1000, 9999)}", 
                            BusType = random.Next(3) == 0 ? "AC Sleeper" : (random.Next(2) == 0 ? "AC Seater" : "Non-AC Sleeper"), 
                            TotalSeats = 40, 
                            OperatorId = op.Id,
                            AssignedRouteId = route.Id
                        };
                        context.Buses.Add(bus);
                        await context.SaveChangesAsync();

                        for (int day = 0; day < 14; day++)
                        {
                            context.Schedules.Add(new Schedule
                            {
                                BusId = bus.Id,
                                RouteId = route.Id,
                                DepartureTime = DateTime.UtcNow.Date.AddDays(day).AddHours(random.Next(6, 23)),
                                ArrivalTime = DateTime.UtcNow.Date.AddDays(day).AddHours(random.Next(24, 32)),
                                Price = random.Next(600, 4500),
                                AvailableSeats = random.Next(5, 41)
                            });
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
            Console.WriteLine("--- Database Seeding Completed ---");
        }
    }
}
