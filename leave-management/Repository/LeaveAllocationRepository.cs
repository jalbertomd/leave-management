using leave_management.Contracts;
using leave_management.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Repository
{
    public class LeaveAllocationRepository : ILeaveAllocationRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveAllocationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CheckAllocation(int leaveTypeId, string employeeId)
        {
            var period = DateTime.Now.Year;
            var leaveAllocations = await FindAll();

            return leaveAllocations.Any(q => q.EmployeeId == employeeId && q.LeaveTypeId == leaveTypeId && q.Period == period);
        }

        public async Task<bool> Create(LeaveAllocation entity)
        {
            await _db.LeaveAllocations.AddAsync(entity);
            return await Save();
        }

        public async Task<bool> Delete(LeaveAllocation entity)
        {
            _db.LeaveAllocations.Remove(entity);
            return await Save();
        }

        public async Task<ICollection<LeaveAllocation>> FindAll()
        {
            var leaveAllocations = _db.LeaveAllocations
                .Include(l => l.LeaveType)
                .Include(l => l.Employee)
                .ToListAsync();

            return await leaveAllocations;
        }

        public async Task<LeaveAllocation> FindById(int id)
        {
            var leaveAllocation = _db.LeaveAllocations
                .Include(l => l.LeaveType)
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            return await leaveAllocation;
        }

        public async Task<ICollection<LeaveAllocation>> GetLeaveAllocationsByEmployee(string id)
        {
            var period = DateTime.Now.Year;
            var leaveAllocations = await FindAll();
                
            return leaveAllocations.Where(a => a.EmployeeId == id && a.Period == period)
                .ToList();
        }

        public async Task<LeaveAllocation> GetLeaveAllocationsByEmployeeAndType(string id, int leaveTypeId)
        {
            var period = DateTime.Now.Year;
            var leaveAllocation = await FindAll();
                
            return leaveAllocation.FirstOrDefault(a => a.EmployeeId == id && a.Period == period && a.LeaveTypeId == leaveTypeId);
        }

        public async Task<bool> IsExists(int id)
        {
            var exists = _db.LeaveAllocations.AnyAsync(l => l.Id == id);
            return await exists;
        }

        public async Task<bool> Save()
        {
            int changes = await _db.SaveChangesAsync();
            return changes > 0;
        }

        public async Task<bool> Update(LeaveAllocation entity)
        {
            _db.Update(entity);
            return await Save();
        }
    }
}
