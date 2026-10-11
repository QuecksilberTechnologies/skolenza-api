using AutoMapper;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOS.Common;
using skolenza.application.Features.DepartmentCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using skolenza.domain.Entity; using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Department
{

    public class GetDepartmentOptionResponse 
    {

        public  int  Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
       // public bool IsActive { get; set; }
    
    }

}
