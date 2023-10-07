using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HHI.API.Helpers;
using HHI.API.Business.Models;
using HHI.API.Business.Models.Dto;
using HHI.API.Business.Models.Command.Find;
using HHI.API.Business.Models.Command.Create;
using HHI.API.Business.Models.Command;
using HHI.API.Business.Service.Users;
using HHI.API.Business.Service.MRP;
using HHI.API.Business.Helpers;
using HHI.API.Models.ListProfiles;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace HHI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MRPController : ERPApiController
    {
        private IMRPDataService _DataService;

        public MRPController(IMRPDataService dataService, IUserDataService userDataService) : base(userDataService)
        {
            _DataService = dataService;
        }

        [HttpGet("GetMRPDashboard", Name = "GetMRPDashboard")]
        [ProducesResponseType(typeof(Response<List<MRPProductDto>>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetExpoDashboard(int calling_user_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetDashboard();

            return new JsonResult(result);
        }

        [HttpGet("GetVendorOpenPOOrders", Name = "GetVendorOpenPOOrders")]
        [ProducesResponseType(typeof(Response<List<VendorOpenPOOrdersDto>>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetVendorOpenPOOrders(int vendor_id, int calling_user_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetVendorOpenPOOrders(vendor_id);

            return new JsonResult(result);
        }

        [HttpGet("GetOpenPOVendors", Name = "GetOpenPOVendors")]
        [ProducesResponseType(typeof(Response<List<OpenPOVendorsDto>>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetOpenPOVendors(int calling_user_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetOpenPOVendors();

            return new JsonResult(result);
        }

        [HttpGet("GetPulledItemsDashboard", Name = "GetPulledItemsDashboard")]
        [ProducesResponseType(typeof(Response<MRPPulledItemsDashboard>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetPulledItemsDashboard(int calling_user_id, int? category_id)
        {
         var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetPulledItemsDashboard(category_id);

            return new JsonResult(result);
        }

        [HttpGet("GetLinesFromMaterial", Name = "GetLinesFromMaterial")]
        [ProducesResponseType(typeof(Response<LinesFromMaterialDto>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetLinesFromMaterial(int calling_user_id, string order_num, int kdc_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetLinesFromMaterial(order_num, kdc_id);

            return new JsonResult(result);
        }

        [HttpGet("GetOrderMaterialAllocations", Name = "GetOrderMaterialAllocations")]
        [ProducesResponseType(typeof(Response<MRPMaterialsAllocationDto>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetOrderMaterialAllocations(string order_num, int calling_user_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetOrderMaterialAllocations(order_num, calling_user_id);

            return new JsonResult(result);
        }

        [HttpGet("GetMaterialAllocatedLinesByStockId", Name = "GetMaterialAllocatedLinesByStockId")]
        [ProducesResponseType(typeof(Response<List<MRPMaterialsAllocationDto>>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetMaterialAllocatedLinesByStockId(string stock_id, int calling_user_id)
        {
            var userPermissions = await base.GetUserPermissions(calling_user_id);

            var result = await _DataService.GetMaterialAllocatedLinesByStockId(stock_id, calling_user_id);

            return new JsonResult(result);
        }

        [HttpPost("PullMaterial", Name = "PullMaterial")]
        [ProducesResponseType(typeof(Response<PulledItemDto>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> PullMaterial([FromBody] PullItemCreateCommand command)
        {
            try
            {
                var userPermissions = await base.GetUserPermissions(command.calling_user_id);

                var result = await _DataService.PullItem(command);

                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("RequestMaterialsPO", Name = "RequestMaterialsPO")]
        [ProducesResponseType(typeof(Response<bool>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> RequestMaterialsPO([FromBody] RequestMaterialsCommand command)
        {
            try
            {
                var userPermissions = await base.GetUserPermissions(command.calling_user_id);

                var result = await _DataService.RequestMaterialsPO(command);

                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
		
		// Removed the rest for proprietary
    }
}
