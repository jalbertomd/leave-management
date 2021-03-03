using leave_management.Contracts;
using leave_management.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace leave_management.Repository
{
    public class LeaveRequestRepository : ILeaveRequestRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveRequestRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> Create(LeaveRequest entity)
        {
            await _db.LeaveRequests.AddAsync(entity);
            return await Save();
        }

        public async Task<bool> Delete(LeaveRequest entity)
        {
            _db.LeaveRequests.Remove(entity);
            return await Save();
        }

        public async Task<ICollection<LeaveRequest>> FindAll()
        {
            var leaveRequests = _db.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .Include(r => r.ApprovedBy)
                .Include(r => r.LeaveType)
                .ToListAsync();

            return await leaveRequests;
        }

        public async Task<LeaveRequest> FindById(int id)
        {
            var leaveRequest = _db.LeaveRequests
                .Include(r => r.RequestingEmployee)
                .Include(r => r.ApprovedBy)
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == id);

            return await leaveRequest;
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByEmployee(string employeeId)
        {
            var leaveRequests = await FindAll();

            return leaveRequests.Where(r => r.RequestingEmployeeId == employeeId)
                .ToList(); ;
        }

        public async Task<bool> IsExists(int id)
        {
            var exists = _db.LeaveRequests.AnyAsync(l => l.Id == id);
            return await exists;
        }

        public async Task<bool> Save()
        {
            int changes = await _db.SaveChangesAsync();
            return changes > 0;
        }

        public async Task<bool> Update(LeaveRequest entity)
        {
            _db.Update(entity);
            return await Save();
        }
    }
}
