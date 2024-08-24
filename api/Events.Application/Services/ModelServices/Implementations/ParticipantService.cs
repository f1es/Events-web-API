﻿using AutoMapper;
using Events.Application.Services.ModelServices.Interfaces;
using Events.Domain.Exceptions;
using Events.Domain.Models;
using Events.Domain.Repositories.Interfaces;
using Events.Domain.Shared;
using Events.Domain.Shared.DTO.Request;
using Events.Domain.Shared.DTO.Response;

namespace Events.Application.Services.ModelServices.Implementations;

public class ParticipantService : IParticipantService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IMapper _mapper;
    public ParticipantService(
        IRepositoryManager repositoryManager,
        IMapper mapper)
    {
        _repositoryManager = repositoryManager;
        _mapper = mapper;
    }
    public async Task<ParticipantResponseDto> CreateParticipantAsync(
        Guid eventId,
        Guid userId,
		ParticipantRequestDto participant,
        bool trackChanges)
    {
        var eventModel = await GetEventByIdAndCheckIfExistAsync(eventId, trackChanges);
        var userModel = await GetUserByIdAndCheckIfExistAsync(userId, trackChanges);

        var participantModel = _mapper.Map<Participant>(participant);

        _repositoryManager.Participant.CreateParticipant(eventId, userId, participantModel);

        await _repositoryManager.SaveAsync();

        var participantResponse = _mapper.Map<ParticipantResponseDto>(participantModel);

        return participantResponse;
    }

    public async Task DeleteParticipantAsync(Guid eventId, Guid id, bool trackChanges)
    {
        var eventModel = await GetEventByIdAndCheckIfExistAsync(eventId, trackChanges);

        var participantModel = await GetParticipantByIdAndCheckIfExistAsync(eventId, id, trackChanges);

        _repositoryManager.Participant.DeleteParticipant(participantModel);

        await _repositoryManager.SaveAsync();
    }

    public async Task<IEnumerable<ParticipantResponseDto>> GetAllParticipantsAsync(Guid eventId, Paging paging, bool trackChanges)
    {
        var eventModel = await GetEventByIdAndCheckIfExistAsync(eventId, trackChanges);

        var participantsModels = await _repositoryManager.Participant.GetAllAsync(eventId, paging, trackChanges);

        var participantsResponses = _mapper.Map<IEnumerable<ParticipantResponseDto>>(participantsModels);

        return participantsResponses;
    }

    public async Task<ParticipantResponseDto> GetParticipantByIdAsync(Guid eventId, Guid id, bool trackChanges)
    {
        var eventModel = await GetEventByIdAndCheckIfExistAsync(eventId, trackChanges);

        var participantModel = await GetParticipantByIdAndCheckIfExistAsync(eventId, id, trackChanges);

        var participantResponse = _mapper.Map<ParticipantResponseDto>(participantModel);

        return participantResponse;
    }

    public async Task UpdateParticipantAsync(
        Guid eventId,
        Guid id,
		ParticipantRequestDto participant,
        bool trackChanges)
    {
        var eventModel = await GetEventByIdAndCheckIfExistAsync(eventId, trackChanges);

        var participantModel = await _repositoryManager.Participant.GetByIdAsync(id, trackChanges);

        participantModel = _mapper.Map(participant, participantModel);

        await _repositoryManager.SaveAsync();
    }

    private async Task<Event> GetEventByIdAndCheckIfExistAsync(Guid eventId, bool trackChanges)
    {
        var eventModel = await _repositoryManager.Event.GetByIdAsync(eventId, trackChanges);
        if (eventModel is null)
        {
            throw new NotFoundException($"event with id {eventId} not found");
        }

        return eventModel;
    }
    private async Task<Participant> GetParticipantByIdAndCheckIfExistAsync(Guid eventId, Guid id, bool trackChanges)
    {
        var participantModel = await _repositoryManager.Participant.GetByIdAsync(id, trackChanges);
        if (participantModel is null)
        {
            throw new NotFoundException($"participant with id {id} not found");
        }

        return participantModel;
    }
    private async Task<User> GetUserByIdAndCheckIfExistAsync(Guid userId, bool trackChanges)
    {
        var userModel = await _repositoryManager.User.GetByIdAsync(userId, trackChanges);
        if (userModel is null)
        {
            throw new NotFoundException($"user with id {userId} not found");
        }

        return userModel;
    }
}
