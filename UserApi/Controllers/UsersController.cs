using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApi.Models;
using UserApi.Models.Dtos;
using UserApi.Services;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    // GET /api/users  → tüm kullanıcıları listele
    [HttpGet]
    public async Task<List<User>> Get() =>
        await _userService.GetAllAsync();

    // GET /api/users/{id}  → tek kullanıcı getir
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
            return NotFound();
        return user;
    }

    // POST /api/users  → yeni kullanıcı ekle
    [HttpPost]
    public async Task<ActionResult<User>> Create(User user)
    {
        await _userService.CreateAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    // PUT /api/users/{id}  → kullanıcıyı güncelle
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserRequest request)
    {
        var updated = await _userService.UpdateAsync(id, request.Name, request.Email);
        if (!updated)
            return NotFound();
        return NoContent();
    }

    // DELETE /api/users/{id}  → kullanıcıyı sil
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _userService.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
