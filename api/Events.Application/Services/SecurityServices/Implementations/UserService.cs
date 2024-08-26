﻿using AutoMapper;
using Events.Application.Extensions;
using Events.Domain.Repositories.Interfaces;
using Events.Application.Services.SecurityServices.Interfaces;
using Events.Domain.Enums;
using Events.Domain.Exceptions;
using Events.Domain.Models;
using Events.Domain.Shared.DTO.Request;
using Events.Domain.Shared.DTO.Response;
using FluentValidation;

namespace Events.Application.Services.SecurityServices.Implementations;

public class UserService : IUserService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshProvider _refreshProvider;
    private readonly IRefreshTokenService _refreshTokenService;
    public UserService(
        IRepositoryManager repositoryManager,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        IJwtProvider jwtProvider,
        IRefreshProvider refreshProvider,
        IRefreshTokenService refreshTokenService)
    {
        _repositoryManager = repositoryManager;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _jwtProvider = jwtProvider;
        _refreshProvider = refreshProvider;
        _refreshTokenService = refreshTokenService;
    }
    public async Task<(string accessToken, RefreshToken refreshToken)> LoginUserAsync(
        UserLoginRequestDto user,
        bool trackUserChanges,
        bool trackRefreshTokenChanges)
    {
        var userModel = await _repositoryManager.User.GetByUsernameAsync(user.Username, trackUserChanges);
        if (userModel is null)
        {
			throw new UnauthorizedException("Failed to login");
		}

        var verificationResult = _passwordHasher.VerifyPassword(user.Password, userModel.PasswordHash);
        if (!verificationResult)
        {
            throw new UnauthorizedException("Failed to login"); 
        }

        var accessToken = _jwtProvider.GenerateToken(userModel);
        var refreshToken = _refreshProvider.GenerateToken(userModel.Id);

        await _refreshTokenService.UpdateRefreshToken(
            userModel.Id,
            refreshToken,
            trackRefreshTokenChanges);


        return (accessToken, refreshToken);
    }

    public async Task RegisterUserAsync(UserRegisterRequestDto user, bool trackChanges)
    {
        var existUser = await _repositoryManager.User.GetByUsernameAsync(user.Username, trackChanges);
        if (existUser != null)
        {
            throw new AlreadyExistsException($"user with username {user.Username} already exist");
        }

        var passwordHash = _passwordHasher.GenerateHash(user.Password);

        var userModel = _mapper.Map<User>(user);

        userModel.PasswordHash = passwordHash;
        
        userModel.Role = Roles.user.ToString();

        _repositoryManager.User.Create(userModel);
        await _refreshTokenService.CreateRefreshTokenAsync(userModel.Id);

        await _repositoryManager.SaveAsync();
    }

    public async Task GrantRoleForUserAsync(Guid id, string role, bool trackChanges)
    {
		var user = await _repositoryManager.User.GetByIdAsync(id, trackChanges);
		if (user is null)
		{
			throw new NotFoundException($"user with id {id} not found");
		}

        var verifiedRole = GetRoleIfExist(role);

        user.Role = verifiedRole;

        await _repositoryManager.SaveAsync();
	}

    private string GetRoleIfExist(string role)
    {
        role = role.ToLower();

        switch(role)
        {
            case nameof(Roles.admin):
                return Roles.admin.ToString();
            case nameof(Roles.user): 
                return Roles.user.ToString();
            case nameof(Roles.manager): 
                return Roles.manager.ToString();
            default:
                throw new BadRequestException($"Role {role} doesn't exist");
        }
    }

    public async Task<UserResponseDto> GetUserByIdAsync(Guid id, bool trackChanges)
    {
        var user = await _repositoryManager.User.GetByIdAsync(id, trackChanges);
        if (user is null)
        {
            throw new NotFoundException($"user with id {id} not found");
        }

        var userResponse = _mapper.Map<UserResponseDto>(user);

        return userResponse;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync(Paging paging, bool trackChanges)
    {
        var users = await _repositoryManager.User.GetAllAsync(paging, trackChanges);

        var usersResponse = _mapper.Map<IEnumerable<UserResponseDto>>(users);

        return usersResponse;
    }
}
