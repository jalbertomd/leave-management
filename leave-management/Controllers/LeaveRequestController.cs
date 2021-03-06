﻿using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        //private readonly ILeaveRequestRepository _leaveRequestRepo;
        //private readonly ILeaveTypeRepository _leaveTypeRepo;
        //private readonly ILeaveAllocationRepository _leaveAllocRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(
            //ILeaveRequestRepository leaveRequestRepo,
            //ILeaveTypeRepository leaveTypeRepo,
            //ILeaveAllocationRepository leaveAllocRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            //_leaveRequestRepo = leaveRequestRepo;
            //_leaveTypeRepo = leaveTypeRepo;
            //_leaveAllocRepo = leaveAllocRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles ="Administrator")]
        // GET: LeaveRequestController
        public async Task<ActionResult> Index()
        {
            //var leaveRequests = await _leaveRequestRepo.FindAll();
            var leaveRequests = await _unitOfWork.LeaveRequests.FindAll(
                includes: new List<string> { "RequestingEmployee", "LeaveTypes"}
                );
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestViewModel>>(leaveRequests);
            var model = new AdminLeaveRequestViewViewModel
            {
                TotalRequests = leaveRequestsModel.Count,
                ApprovedRequests = leaveRequestsModel.Count(r => r.Approved == true),
                RejectedRequests = leaveRequestsModel.Count(r => r.Approved == false),
                PendingRequests = leaveRequestsModel.Count(r => r.Approved == null),
                LeaveRequests = leaveRequestsModel
            };

            return View(model);
        }

        // GET: LeaveRequestController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            //var leaveRequest = await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(r =>r.Id == id,
                includes: new List<string> { "ApprovedBy", "RequestingEmployee", "LeaveType" });
            var model = _mapper.Map<LeaveRequestViewModel>(leaveRequest);
            return View(model);
        }

        // GET: LeaveRequestController/Create
        public async Task<ActionResult> Create()
        {
            //var leaveTypes = await _leaveTypeRepo.FindAll();
            var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();

            var leaveTypeItems = leaveTypes.Select(l => new SelectListItem 
            { 
                Value = l.Id.ToString(), 
                Text = l.Name 
            });
            var model = new CreateLeaveRequestViewModel
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequestController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLeaveRequestViewModel model)
        {
            try
            {
                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);
                var period = DateTime.Now.Year;

                //var leaveTypes = await _leaveTypeRepo.FindAll();
                var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();

                var leaveTypeItems = leaveTypes.Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Name
                });

                model.LeaveTypes = leaveTypeItems;

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if(DateTime.Compare(startDate, endDate) > 1)
                {
                    ModelState.AddModelError("", "Start Date cannot be further in the future that End Date.");
                    return View(model);
                }

                var employee = await _userManager.GetUserAsync(User);
                //var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);
                var allocation = await _unitOfWork.LeaveAllocations.Find(a => a.EmployeeId == employee.Id &&
                a.Period == period &&
                a.LeaveTypeId == model.LeaveTypeId);

                int daysRequested = (int)(endDate.Date - startDate.Date).TotalDays;

                if(daysRequested > allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You do not have sufficient days for this request.");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestViewModel
                {
                    LeaveTypeId = model.LeaveTypeId,
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    RequestComments = model.RequestComments
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                //var isSuccess = await _leaveRequestRepo.Create(leaveRequest);

                //if(!isSuccess)
                //{
                //    ModelState.AddModelError("", "Something went wrong with your request.");
                //    return View(model);
                //}

                await _unitOfWork.LeaveRequests.Create(leaveRequest);
                await _unitOfWork.Save();

                return RedirectToAction("MyLeave");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", $"Something went wrong. \nError: {ex.Message}");
                return View(model);
            }
        }

        // GET: LeaveRequestController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: LeaveRequestController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Delete/5
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

        public async Task<ActionResult> ApproveRequest(int id)
        {
            try
            {
                var employee = await _userManager.GetUserAsync(User);
                //var leaveRequest = await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(r => r.Id == id);
                var employeeId = leaveRequest.RequestingEmployeeId;
                var leaveTypeId = leaveRequest.LeaveTypeId;
                var period = DateTime.Now.Year;

                //var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(leaveRequest.RequestingEmployeeId, leaveRequest.LeaveTypeId);
                var allocation = await _unitOfWork.LeaveAllocations.Find(a => a.EmployeeId == employeeId &&
                a.Period == period &&
                a.LeaveTypeId == leaveTypeId);

                int daysRequested = (int)(leaveRequest.StartDate - leaveRequest.EndDate).TotalDays;

                allocation.NumberOfDays -= daysRequested;

                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = employee.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaveRequestRepo.Update(leaveRequest);
                //await _leaveAllocRepo.Update(allocation);

                _unitOfWork.LeaveRequests.Update(leaveRequest);
                _unitOfWork.LeaveAllocations.Update(allocation);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }            
        }

        public async Task<ActionResult> RejectRequest(int id)
        {
            try
            {
                var employee = await _userManager.GetUserAsync(User);
                //var leaveRequest = await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(r => r.Id == id);

                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = employee.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaveRequestRepo.Update(leaveRequest);
                _unitOfWork.LeaveRequests.Update(leaveRequest);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<ActionResult> MyLeave()
        {
            var employee = await _userManager.GetUserAsync(User);
            var employeeId = employee.Id;
            //var employeeAllocations = await _leaveAllocRepo.GetLeaveAllocationsByEmployee(employeeId);
            var employeeAllocations = await _unitOfWork.LeaveAllocations.FindAll(a => a.EmployeeId == employeeId, 
                includes: new List<string> { "LeaveType"});

            //var employeeRequests = await _leaveRequestRepo.GetLeaveRequestsByEmployee(employeeId);
            var employeeRequests = await _unitOfWork.LeaveRequests.FindAll(r => r.RequestingEmployeeId == employeeId);

            var employeeAllocationsModel = _mapper.Map<List<LeaveAllocationViewModel>>(employeeAllocations);
            var employeeRequestsModel = _mapper.Map<List<LeaveRequestViewModel>>(employeeRequests);

            var model = new EmployeeLeaveRequestViewModel
            {
                LeaveAllocations = employeeAllocationsModel,
                LeaveRequests = employeeRequestsModel
            };

            return View(model);
        }

        public async Task<ActionResult> CancelRequest(int id)
        {
            //var leaveRequest = await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(r => r.Id == id);

            leaveRequest.Cancelled = true;

            //await _leaveRequestRepo.Update(leaveRequest);
            _unitOfWork.LeaveRequests.Update(leaveRequest);
            await _unitOfWork.Save();

            return RedirectToAction("MyLeave");
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
