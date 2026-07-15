using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Dtos;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController(IOrderService orders) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        var order = await orders.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id)
        => Ok(await orders.GetAsync(CurrentUserId, id));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Update(Guid id, UpdateOrderRequest request)
        => Ok(await orders.UpdateAsync(CurrentUserId, id, request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await orders.DeleteAsync(CurrentUserId, id);
        return NoContent();
    }
}
