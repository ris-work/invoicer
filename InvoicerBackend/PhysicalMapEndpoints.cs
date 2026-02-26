using System.Data;
using common;
using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class PhysicalMapEndpoints
    {
        public static WebApplication AddPhysicalMapEndpoints(this WebApplication app)
        {
            // Save Map (Upsert Map and Replace Locations)
            app.AddAsyncEndpointWithBearerAuth<MapSaveDto, PhysicalMap>(
                "SaveMap",
                async (DataIn, LoginInfo) =>
                {
                    var Data = (MapSaveDto)DataIn;
                    System.Console.WriteLine($"SaveMap: User {LoginInfo.UserId}, Map {Data.Map.MapId}");

                    using (var ctx = new NewinvContext())
                    {
                        using var tx = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                        try
                        {
                            // 1. Handle PhysicalMap
                            PhysicalMap dbMap;

                            if (Data.Map.MapId > 0)
                            {
                                // Update existing
                                dbMap = await ctx.PhysicalMaps.FirstOrDefaultAsync(m => m.MapId == Data.Map.MapId);
                                if (dbMap != null)
                                {
                                    dbMap.MapName = Data.Map.MapName;
                                    dbMap.MapType = Data.Map.MapType;
                                    dbMap.Map = Data.Map.Map; // Base64 Image
                                    dbMap.VerticalGridlines = Data.Map.VerticalGridlines;
                                    dbMap.HorizontalGridlines = Data.Map.HorizontalGridlines;
                                }
                                else
                                {
                                    // ID provided but not found, treat as new or error. Here we treat as new.
                                    ctx.PhysicalMaps.Add(Data.Map);
                                    dbMap = Data.Map;
                                }
                            }
                            else
                            {
                                // Create new
                                ctx.PhysicalMaps.Add(Data.Map);
                                dbMap = Data.Map;
                            }

                            // Save to ensure MapId is generated for new maps
                            await ctx.SaveChangesAsync();

                            // 2. Handle MappedLocations (Replace Strategy)
                            // Remove all existing locations for this map
                            var existingLocations = await ctx.MappedLocations
                                .Where(l => l.MapId == dbMap.MapId)
                                .ToListAsync();

                            if (existingLocations.Any())
                            {
                                ctx.MappedLocations.RemoveRange(existingLocations);
                            }

                            // Add new locations
                            if (Data.Locations != null && Data.Locations.Any())
                            {
                                foreach (var loc in Data.Locations)
                                {
                                    loc.MapId = dbMap.MapId; // Ensure correct linkage
                                    ctx.MappedLocations.Add(loc);
                                }
                            }

                            await ctx.SaveChangesAsync();
                            await tx.CommitAsync();

                            return dbMap;
                        }
                        catch
                        {
                            await tx.RollbackAsync();
                            throw;
                        }
                    }
                },
                "Refresh"
            );

            // Get Map (Fetch Map and Locations)
            // FIX: Changed AddEndpointWithBearerAuth to AddAsyncEndpointWithBearerAuth to support async/await
            app.AddAsyncEndpointWithBearerAuth<long, MapSaveDto>(
                "GetMap",
                async (MapIdIn, LoginInfo) =>
                {
                    var MapId = (long)MapIdIn;
                    using (var ctx = new NewinvContext())
                    {
                        var map = await ctx.PhysicalMaps.FirstOrDefaultAsync(m => m.MapId == MapId);
                        if (map == null) return null;

                        var locations = await ctx.MappedLocations
                            .Where(l => l.MapId == MapId)
                            .ToListAsync();

                        return new MapSaveDto
                        {
                            Map = map,
                            Locations = locations
                        };
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}