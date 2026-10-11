using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class LoginRequestDTO
    {
        public required string LoginId { get; set; }          // User's login ID       
        public required string Password { get; set; }         // User's password
        public string MacAddress { get; set; }       // User's Mac Address (optional)
        public string IpAddressPublic { get; set; }        // User's IP Address (optional)
        public required string IpAddressLocal { get; set; }        // User's IP Address (optional)
        public double Latitude { get; set; }          // User's latitude for geolocation (optional)
        public double Longitude { get; set; }         // User's longitude for geolocation (optional)
        public int LoginDevice { get; set; }         // The device being used for login (optional)
    }

}
