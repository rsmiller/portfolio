using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HHI.API.Business.Base;
using HHI.API.Business.DatabaseModels.KDC.Inspections;
using HHI.API.Business.DatabaseModels.KDC.Users;
using HHI.API.Business.Extensions;
using HHI.API.Business.Helpers;
using HHI.API.Business.Interfaces;
using HHI.API.Business.Models;
using HHI.API.Business.Models.Dto;
using HHI.API.Business.Models.Command.Create;
using HHI.API.Business.Models.Command.Delete;
using HHI.API.Business.Models.Command.Edit;
using HHI.API.Business.Models.Command.Find;
using HHI.API.Business.Models.Dto.List;
using HHI.API.Business.Models.Command;
using HHI.API.Business.DatabaseModels.KDC.General;
using HHI.API.Business.DatabaseModels.KDC.Products;
using HHI.API.Business.Service.Sales;

namespace HHI.API.Business.Service.Inspections
{
    public interface IInternalInspectionsDataService : IERPModuleInterface<InternalInspection, InternalInspectionDto, InternalInspectionsListDto, InternalInspectionCreateCommand, InternalInspectionEditCommand, InternalInspectionDeleteCommand, InternalInspectionFindCommand>
    {
        Task<Response<InternalInspectionLineDto>> GetLineDto(int object_id, UserPermissionsSet callingUserPermissions);
        Task<Response<InternalInspectionTypeDataDto>> DownloadTypeData();
        Task<Response<InternalInspectionLineDto>> CreateLine(InternalInspectionLineCreateCommand createCommandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<InternalInspectionLineDto>> EditLine(InternalInspectionLineEditCommand editCommandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<InternalInspectionSerialDto>> CreateSerial(InternalInspectionSerialCreateCommand createCommandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<InternalInspectionSerialDto>> EditSerial(InternalInspectionSerialEditCommand editCommandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<List<InternalSerialSeachResultDto>>> SerialSearch(string serial_num);
        Task<Response<InternalInspectionLineDto>> DeleteLine(InternalInspectionLineDeleteCommand commandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<InternalInspectionSerialDto>> DeleteSerial(InternalInspectionSerialDeleteCommand commandModel, UserPermissionsSet callingUserPermissions);
        Task<Response<List<InternalInspectionSerialEncounterDto>>> GetEncountersBySerialNumber(string serial_num);
        Task<Response<OrderDto>> ConvertInspectionToQuote(InternalInspectionConvertToQuoteCommand commandModel, UserPermissionsSet callingUserPermissions);
    }

    public class InternalInspectionsDataService : ERPModuleBase, IInternalInspectionsDataService
    {
        private IHHIContext _IContext;
        private IOrderDataService _IOrderDataService;
        private IOrderDetailDataService _IOrderDetailsDataService;
        private ISendMailDataService _SendMailDataService;

        public event ModuleCreatedEvent ModelCreated;
        public event ModuleEditedEvent ModelEdited;
        public event ModuleDeletedEvent ModelDeleted;

        public override string ModuleName => "Internal Inspections";
        public override Guid ModuleIdentifier => Guid.Parse("787774d6-dc39-40eb-b19d-4229b9dd80f9");
        public override int Version => 1;

        public InternalInspectionsDataService(IHHIContext context, IOrderDataService quoteDataService, IOrderDetailDataService orderDetailsDataService, ISendMailDataService mailDataService) : base(context)
        {
            _IContext = context;
            _IOrderDataService = quoteDataService;
            _IOrderDetailsDataService = orderDetailsDataService;
            _SendMailDataService = mailDataService;
        }


        public InternalInspection Get(int object_id)
        {
            return _IContext.InternalInspections.SingleOrDefault(m => m.id == object_id);
        }

        public async Task<InternalInspection> GetAsync(int object_id)
        {
            return await _IContext.InternalInspections.SingleOrDefaultAsync(m => m.id == object_id);
        }

        public async Task<Response<InternalInspectionDto>> GetDto(int object_id, UserPermissionsSet callingUserPermissions)
        {
            Response<InternalInspectionDto> response = new Response<InternalInspectionDto>();

            try
            {
                var inspection = await GetAsync(object_id);
                if(inspection == null)
                    return new Response<InternalInspectionDto>(ResultCode.NotFound);

                var lines = await _IContext.InternalInspectionLines.Where(m => m.internal_inspections_id == object_id && m.is_deleted == false).ToListAsync();
                var serials = await _IContext.InternalInspectionSerials.Where(m => m.internal_inspections_id == object_id && m.is_deleted == false).ToListAsync();

                var inspection_dto = await MapToDtoAsync(inspection);

                foreach (var line in lines)
                {
                    var line_dto = await MapToInspectionLineDto(line, false);
                    inspection_dto.lines.Add(line_dto);
                }

                foreach (var serial in serials)
                {
                    var serial_dto = await MapToInspectionSerialDto(serial, false);
                    inspection_dto.serials.Add(serial_dto);
                }

                // Get all the unique employee numbers
                var line_created_bys = lines.Select(m => m.created_by).Distinct().ToList();
                var line_updated_bys = lines.Select(m => m.updated_by).Distinct().ToList();
                var serial_created_bys = serials.Select(m => m.created_by).Distinct().ToList();

                // Merge them all
                var mass_emps = mass_emps.Union(line_created_bys).Union(line_updated_bys).Union(serial_created_bys).Where(m => m != "").ToList();
		// Now catch them all like pokemon
  		var employees = await _IContext.Users.Where(m => mass_emps.Contains(m.employee_number)).SingleOrDefaultAsync();

		// Add details
		foreach (var line in inspection_dto.lines)
		{
  			var employee = employees.FirstOrDefault(m => m == line.created_by);
     			if(employee != null)
			{
				if (line.created_by == emp_no_int)
			    	    line.created_by_name = employee.first_name.FirstCharToUpper() + " " + employee.last_name.FirstCharToUpper();
				if (line.updated_by == emp_no_int)
			    	    line.updated_by_name = employee.first_name.FirstCharToUpper() + " " + employee.last_name.FirstCharToUpper();
	    		}
		}
		
		foreach (var serial in inspection_dto.serials)
		{
  			var employee = employees.FirstOrDefault(m => m == line.created_by);
     			if(employee != null)
			{
	   			if (serial.created_by == emp_no_int)
				    serial.created_by_name = employee.first_name.FirstCharToUpper() + " " + employee.last_name.FirstCharToUpper();
				
				var associated_line = inspection_dto.lines.FirstOrDefault(m => m.id == serial.internal_inspections_lines_id);
				if (associated_line != null)
				    associated_line.serial_number = serial.serial_num;
		 	}
		}
  
                response.Data = inspection_dto;
            }
            catch (Exception e)
            {
                response.SetException(e);
                await _IContext.SpPostError(50, "InternalInspectionsDataService:GetDto", e.Message, e.InnerException != null ? e.InnerException.Message : "");
            }

            return response;
        }

        public async Task<Response<InternalInspectionLineDto>> GetLineDto(int object_id, UserPermissionsSet callingUserPermissions)
        {
            Response<InternalInspectionLineDto> response = new Response<InternalInspectionLineDto>();
            var result = await _IContext.InternalInspectionLines.SingleOrDefaultAsync(m => m.id == object_id);

            if (result == null)
                return new Response<InternalInspectionLineDto>(ResultCode.NotFound);

            response.Data = await this.MapToInspectionLineDto(result);

            var serials = await _IContext.InternalInspectionSerials.Where(m => m.internal_inspections_lines_id == result.id).ToListAsync();
            foreach(var serial in serials)
                response.Data.serial_numbers.Add(await this.MapToInspectionSerialDto(serial));

            if (serials.Count() > 0)
                response.Data.serial_number = response.Data.serial_numbers.First().serial_num;

            return response;
        }

        public async Task<Response<InternalInspectionDto>> Create(InternalInspectionCreateCommand commandModel, UserPermissionsSet callingUserPermissions)
        {
            if (commandModel == null)
                return new Response<InternalInspectionDto>(ResultCode.NullItemInput);

            var validationResult = ModelValidationHelper.ValidateModel(commandModel);
            if (!validationResult.Success)
                return new Response<InternalInspectionDto>(validationResult.Exception);

            try
            {
                var alreadyExists = await InspectionExists(commandModel);
                if (alreadyExists == true)
                    return new Response<InternalInspectionDto>(ResultCode.AlreadyExists);


                var model = await MapForCreate(commandModel);


                await _IContext.InternalInspections.AddAsync(model);
                await _IContext.SaveChangesAsync();

                var dto = await MapToDtoAsync(model);

                return new Response<InternalInspectionDto>(dto);
            }
            catch (Exception e)
            {
                await _IContext.SpPostError(50, "InternalInspectionsDataService:Create", e.Message, e.InnerException != null ? e.InnerException.Message : "");
                return new Response<InternalInspectionDto>(e.Message);
            }
        }

        public async Task<Response<InternalInspectionDto>> Edit(InternalInspectionEditCommand commandModel, UserPermissionsSet callingUserPermissions)
        {
            if (commandModel == null)
                return new Response<InternalInspectionDto>(ResultCode.NullItemInput);

            var validationResult = ModelValidationHelper.ValidateModel(commandModel);
            if (!validationResult.Success)
                return new Response<InternalInspectionDto>(validationResult.Exception);

            var existingEntity = await GetAsync(commandModel.id);
            if (existingEntity == null)
                return new Response<InternalInspectionDto>(ResultCode.NotFound);

            Response<InternalInspectionDto> response = new Response<InternalInspectionDto>();

            try
            {
                // Strings
                if (!string.IsNullOrEmpty(commandModel.quote_num) && existingEntity.quote_num != commandModel.quote_num)
                    existingEntity.quote_num = commandModel.quote_num;
                if (!string.IsNullOrEmpty(commandModel.description) && existingEntity.description != commandModel.description)
                    existingEntity.description = commandModel.description;
                if (!string.IsNullOrEmpty(commandModel.tag_number) && existingEntity.tag_number != commandModel.tag_number)
                    existingEntity.tag_number = commandModel.tag_number;
                if (!string.IsNullOrEmpty(commandModel.department) && existingEntity.department != commandModel.department)
                    existingEntity.department = commandModel.department;


                /// Numbers
                if (commandModel.sales_person_num.HasValue && existingEntity.sales_person_num != commandModel.sales_person_num)
                    existingEntity.sales_person_num = commandModel.sales_person_num;
                if (commandModel.qty.HasValue && existingEntity.qty != commandModel.qty)
                    existingEntity.qty = commandModel.qty.Value;

                // Bools
                if (commandModel.is_pre_inspection.HasValue && existingEntity.is_pre_inspection != commandModel.is_pre_inspection)
                    existingEntity.is_pre_inspection = commandModel.is_pre_inspection.Value;
                if (commandModel.is_canceled.HasValue && existingEntity.is_canceled != commandModel.is_canceled)
                    existingEntity.is_canceled = commandModel.is_canceled.Value;

                // Conditionals
                if (commandModel.cust_id.HasValue && existingEntity.cust_id != commandModel.cust_id.ToString())
                {
                    var customer = await _IContext.Customers.FirstOrDefaultAsync(m => m.cust_id == commandModel.cust_id.Value);
                    existingEntity.customer_name = customer.customer_name.ToUpper();
                    existingEntity.cust_id = commandModel.cust_id.Value.ToString();
                }

                if (commandModel.receieved_by.HasValue && existingEntity.receieved_by != commandModel.receieved_by)
                {
                    existingEntity.receieved_by = commandModel.receieved_by;
                    existingEntity.received_on = DateTime.Now;
                }

                if (commandModel.inspection_status.HasValue && existingEntity.inspection_status != commandModel.inspection_status)
                {
                    existingEntity.inspection_status = commandModel.inspection_status.Value;

                    if (commandModel.inspection_status == InternalInspectionStatus.COMPLETE_INSPECTION)
                    {
                        existingEntity.is_complete = true;
                        existingEntity.completed_on = DateTime.Now;
                        existingEntity.completed_by = commandModel.calling_user_id;

                        if (existingEntity.sales_person_num.HasValue)
                        {
                            var employee = await _IContext.Users.FirstAsync(m => m.employee_number == existingEntity.sales_person_num.Value.ToString());

                            if (!String.IsNullOrEmpty(employee.email))
                                await _SendMailDataService.SendSalesPersonEmailAboutInternalInspectionCompletion(employee.email, existingEntity.id.ToString(), existingEntity.customer_name);
                        }
                        else
                        {
                            // Send email blast to distribution group
                            await _SendMailDataService.SendSalesPeopleEmailAboutInternalInspectionCompletion(existingEntity.id.ToString(), existingEntity.customer_name);
                        }
                    }

                    if (commandModel.inspection_status == InternalInspectionStatus.COMPLETE_SALES)
                    {
                        existingEntity.is_sales_complete = true;
                        existingEntity.sales_completed_on = DateTime.Now;
                        existingEntity.sales_completed_by = commandModel.calling_user_id;
                    }
                }

                existingEntity.updated_by = commandModel.calling_user_id;
                existingEntity.updated_on = DateTime.Now;

                _IContext.InternalInspections.Update(existingEntity);
                await _IContext.SaveChangesAsync();


                var dto_response = await this.GetDto(existingEntity.id, callingUserPermissions);

                response.Data = dto_response.Data;
            }
            catch (Exception e)
            {
                await _IContext.SpPostError(50, "InternalInspectionsDataService:Edit", e.Message, e.InnerException != null ? e.InnerException.Message : "");
                return new Response<InternalInspectionDto>(e.Message);
            }
            

            return response;
        }

        public async Task<Response<InternalInspectionDto>> Delete(InternalInspectionDeleteCommand commandModel, UserPermissionsSet callingUserPermissions)
        {
            if (commandModel == null)
                return new Response<InternalInspectionDto>(ResultCode.NullItemInput);

            if (callingUserPermissions == null)
                return new Response<InternalInspectionDto>(ResultCode.NullItemInput);

            var validationResult = ModelValidationHelper.ValidateModel(commandModel);
            if (!validationResult.Success)
                return new Response<InternalInspectionDto>(validationResult.Exception);

            var user = await _IContext.Users.FirstOrDefaultAsync(m => m.employee_number == commandModel.calling_user_id.ToString());
            if (user == null)
                return new Response<InternalInspectionDto>(ResultCode.NotFound);

            if (!base.UserHasPermission(callingUserPermissions.permission_internal_inspections, ModulePermission.Delete))
                return new Response<InternalInspectionDto>(ResultCode.InvalidPermission);

            var existingEntity = await GetAsync(commandModel.id);
            if (existingEntity == null)
                return new Response<InternalInspectionDto>(ResultCode.NotFound);


            Response<InternalInspectionDto> response = new Response<InternalInspectionDto>();


            existingEntity.is_canceled = true;
            existingEntity.inspection_status = InternalInspectionStatus.DELETED;
            existingEntity.updated_on = DateTime.Now;
            existingEntity.updated_by = commandModel.calling_user_id;

            _IContext.InternalInspections.Update(existingEntity);
            await _IContext.SaveChangesAsync();

            var dto = await this.MapToDtoAsync(existingEntity);

            response.Data = dto;

            return response;
        }

		// Removed the rest for proprietary

	}
}
