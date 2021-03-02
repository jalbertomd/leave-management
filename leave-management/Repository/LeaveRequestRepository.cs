using leave_management.Contracts;
using leave_management.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Repository
{
    public class LeaveRequestRepository : ILeaveRequestRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveRequestRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool Create(LeaveRequest entity)
        {
            _db.LeaveRequests.Add(entity);
            return Save();
        }

        public bool Delete(LeaveRequest entity)
        {
            _db.LeaveRequests.Remove(entity);
            return Save();
        }

        public ICollection<LeaveRequest> FindAll()
        {
            var leaveRequests = _db.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .Include(r => r.ApprovedBy)
                .Include(r => r.LeaveType)
                .ToList();
            return leaveRequests;
        }

        public LeaveRequest FindById(int id)
        {
            var leaveRequest = _db.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .Include(r => r.ApprovedBy)
                .Include(r => r.LeaveType)
                .FirstOrDefault(r => r.Id == id);
            return leaveRequest;
        }

        public IEnumerable<LeaveRequest> GetLeaveRequestsByEmployee(string employeeId)
        {
            var leaveRequests = FindAll()
                .Where(r => r.RequestingEmployeeId == employeeId)
                .ToList();

            return leaveRequests;
        }

        public bool IsExists(int id)
        {
            var exists = _db.LeaveRequests.Any(l => l.Id == id);
            return exists;
        }

        public bool Save()
        {
            int changes = _db.SaveChanges();
            return changes > 0;
        }

        public bool Update(LeaveRequest entity)
        {
            _db.Update(entity);
            return Save();
        }
    }
}
