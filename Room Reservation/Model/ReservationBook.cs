using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Documents;
using Room_Reservation.Exceptions;

namespace Room_Reservation.Model
{
    public class ReservationBook
    {
        private readonly Dictionary<RoomID, List<Reservation>> _roomsToReservation;
        private readonly List<Reservation> _reservations;

        public ReservationBook()
        {
            _roomsToReservation = new Dictionary<RoomID, List<Reservation>>();
            _reservations = new List<Reservation>();
        }

        public IEnumerable<Reservation> GetReservationForUser(string username)
        {
            return _reservations.Where(r => r.Username == username);
        }

        public void AddReservation(Reservation reservation)
        {
            foreach (Reservation existingReservation in _reservations)
            {
                if (existingReservation.Conflicts(reservation))
                {
                    throw new ReservationConflictException(existingReservation, reservation);
                }
            }
            _reservations.Add(reservation);
        }
    }
}
