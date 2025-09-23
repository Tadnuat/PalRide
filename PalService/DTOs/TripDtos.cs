using System;

namespace PalService.DTOs
{
    // Trip DTOs
    public class CreateTripDto
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal? PriceFullRide { get; set; }
        public int SeatTotal { get; set; } = 4;
        public int? VehicleId { get; set; }
        public string? Note { get; set; }
    }

    public class TripDto
    {
        public int TripId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal? PriceFullRide { get; set; }
        public int SeatTotal { get; set; }
        public int SeatAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public DriverInfoDto? Driver { get; set; }
        public DriverInfoDto? Passenger { get; set; }
    }

    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeatCount { get; set; }
    }

    public class DriverInfoDto
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumberMasked { get; set; } = string.Empty;
        public decimal RatingAverage { get; set; }
        public int ReviewsCount { get; set; }
        public bool GmailVerified { get; set; }
        public string? Introduce { get; set; }
    }

    public class SearchTripsDto
    {
        public string? PickupLocation { get; set; }
        public string? DropoffLocation { get; set; }
        public DateTime? StartDate { get; set; }
    }

    // Search history
    public class SearchHistoryItemDto
    {
        public int Id { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PriceRangeDto
    {
        public double DistanceKm { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string Polyline { get; set; } = string.Empty; // optional OSRM polyline
    }

    public class AddVehicleDto
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Car, Bike, etc
        public int SeatCount { get; set; } = 2;
    }

    public class UpdateVehicleDto
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeatCount { get; set; } = 2;
    }

    public class VerifyVehicleDto
    {
        public bool Verified { get; set; }
    }

    public class CreatePassengerRequestDto
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int SeatCount { get; set; } = 1;
        public bool FullRide { get; set; } = false; // Bao xe
        public decimal? OfferedPrice { get; set; } // optional price the passenger suggests
        public string? Note { get; set; }
    }
}


