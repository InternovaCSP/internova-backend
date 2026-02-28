using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internova.Infrastructure.Repositories;

/// <summary>EF Core implementation of IUserRepository.</summary>
public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext _context = context;

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id; // EF Core populates Id after INSERT
    }
}
