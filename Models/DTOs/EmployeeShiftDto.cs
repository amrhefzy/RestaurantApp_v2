using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class EmployeeShiftDto
{
    public int         Id              { get; set; }
    public int         EmployeeId      { get; set; }
    public string      EmployeeNameAr  { get; set; } = string.Empty;
    public string      EmployeeNameEn  { get; set; } = string.Empty;
    public int         TemplateId      { get; set; }
    public string      TemplateNameAr  { get; set; } = string.Empty;
    public string      TemplateNameEn  { get; set; } = string.Empty;
    public DateTime    ShiftDate       { get; set; }
    public DateTime?   ClockIn         { get; set; }
    public DateTime?   ClockOut        { get; set; }
    public DateTime    PlannedStart    { get; set; }
    public DateTime    PlannedEnd      { get; set; }
    public int         OvertimeMinutes { get; set; }
    public ShiftStatus Status          { get; set; }
    public string?     Notes           { get; set; }
}
