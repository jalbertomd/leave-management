using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class LeaveAllocationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveAllocationController(            
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        // GET: LeaveAllocationController
        public async Task<ActionResult> Index()
        {
            var leavetypes = await _unitOfWork.LeaveTypes.FindAll();
            var mappedLeaveTypes = _mapper.Map<List<LeaveType>, List<LeaveTypeViewModel>>(leavetypes.ToList());
            var model = new CreateLeaveAllocationViewModel 
            { 
                LeaveTypes = mappedLeaveTypes, 
                NumberUpdated = 0 
            };

            return View(model);
        }

        public async Task<ActionResult> SetLeave(int id)
        {
            var leaveType = await _unitOfWork.LeaveTypes.Find(t => t.Id == id);
            var employees = await _userManager.GetUsersInRoleAsync("Employee");

            var period = DateTime.Now.Year;

            foreach (var emp in employees)
            {   
                if(await _unitOfWork.LeaveAllocations.IsExists(q => q.EmployeeId == emp.Id && q.LeaveTypeId == id && q.Period == period))                
                    continue;                

                var allocation = new LeaveAllocationViewModel
                {
                    DateCreated = DateTime.Now,
                    EmployeeId = emp.Id,
                    LeaveTypeId = id,
                    NumberOfDays = leaveType.DefaultDays,
                    Period = DateTime.Now.Year
                };

                var leaveAllocation = _mapper.Map<LeaveAllocation>(allocation);
                
                await _unitOfWork.LeaveAllocations.Create(leaveAllocation);
                await _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> ListEmployees()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var model = _mapper.Map<List<EmployeeViewModel>>(employees);

            return View(model);
        }

        // GET: LeaveAllocationController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var emp = await _userManager.FindByIdAsync(id);
            var employee = _mapper.Map<EmployeeViewModel>(emp);
            var period = DateTime.Now.Year;
                        
            var leaveAlloc = await _unitOfWork.LeaveAllocations.FindAll(
                expression: a => a.EmployeeId == id && a.Period == period,
                includes: new List<string> { "LeaveType" });

            var allocations = _mapper.Map<List<LeaveAllocationViewModel>>(leaveAlloc);

            var model = new ViewAllocationViewModel 
            { 
                Employee = employee, 
                EmployeeId = id, 
                LeaveAllocations = allocations 
            };

            return View(model);
        }

        // GET: LeaveAllocationController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveAllocationController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveAllocationController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var allocation = await _unitOfWork.LeaveAllocations.Find(a => a.Id == id,
                includes: new List<string> { "Employee", "LeaveType" });

            var model = _mapper.Map<EditLeaveAllocationViewModel>(allocation);
            return View(model);
        }

        // POST: LeaveAllocationController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditLeaveAllocationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)                
                    return View(model);

                var record = await _unitOfWork.LeaveAllocations.Find(a => a.Id == model.Id);

                record.NumberOfDays = model.NumberOfDays;

                _unitOfWork.LeaveAllocations.Update(record);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", $"Error while saving.\n{ex.Message}");
                return View(model);
            }
        }

        // GET: LeaveAllocationController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveAllocationController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
