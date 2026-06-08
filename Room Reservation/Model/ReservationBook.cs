using System;
using System.Collections.Generic;
using System.Text;

namespace Room_Reservation.Model
{
    public class ReservationBook
    {
        private readonly Dictionary<RoomID, List<Reservation>> _roomsToReservation;

        public ReservationBook()
        {
            _roomsToReservation = new Dictionary<RoomID, List<Reservation>>();
        }
    }
}
