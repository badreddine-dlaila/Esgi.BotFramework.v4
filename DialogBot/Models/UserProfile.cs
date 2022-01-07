﻿using System;

namespace DialogBot.Models
{
    public class UserProfile
    {
        public string   Name            { get; set; }
        public string   Description     { get; set; }
        public DateTime CallbackTime    { get; set; }
        public string   PhoneNumber     { get; set; }
        public string   Bug             { get; set; }
        public string   CorrelationId   { get; set; }
        public DateTime ObservationDate { get; set; }
    }
}