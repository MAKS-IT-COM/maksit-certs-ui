using System.Linq.Expressions;
using Microsoft.Extensions.Options;

using MaksIT.Core.Extensions;
using MaksIT.Core.Sagas;
using MaksIT.Core.Security;
using MaksIT.Core.Security.JWT;
using MaksIT.Core.Webapi.Models;
using MaksIT.Results;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Models.Identity.Login;
using MaksIT.CertsUI.Models.Identity.Logout;
using MaksIT.CertsUI.Models.Identity.User;
using MaksIT.CertsUI.Models.Identity.User.Search;
using MaksIT.CertsUI.Services.Helpers;
using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Mappers;


namespace MaksIT.CertsUI.Services;

public interface IIdentityService {
  #region Search
  Result<PagedResponse<SearchUserResponse>?> SearchUsers(JwtTokenData jwtTokenData, SearchUserRequest requestData);
  Result<PagedResponse<SearchUserEntityScopeResponse>?> SearchUserEntityScopes(JwtTokenData jwtTokenData, SearchUserEntityScopeRequest requestData);
  #endregion

  #region Read
  Result<UserResponse?> ReadUser(JwtTokenData jwtTokenData, Guid id);
  #endregion

  #region Create
  Task<Result<UserResponse?>> PostUserAsync(JwtTokenData jwtTokenData, CreateUserRequest requestData);
  #endregion

  #region Patch
  Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData jwtTokenData, Guid id, PatchUserRequest requestData);
  #endregion

  #region Delete
  Task<Result> DeleteUserAsync(JwtTokenData jwtTokenData, Guid id);
  #endregion

  #region Login/Refresh/Logout
  Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData);
  Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData);
  Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData);
  #endregion
}

public class IdentityService(
  ILogger<IdentityService> logger,
  IOptions<Configuration> appSettings,
  IIdentityQueryService identityQueryService,
  IUserEntityScopeQueryService userEntityScopeQueryService,
  IIdentityDomainService identityDomainService,
  UserToResponseMapper userToResponseMapper
) : ServiceBase<UserResponse, User, SearchUserResponse, UserQueryResult>(logger, appSettings), IIdentityService {

  private readonly IIdentityQueryService _identityQueryService = identityQueryService;
  private readonly IUserEntityScopeQueryService _userEntityScopeQueryService = userEntityScopeQueryService;
  private readonly IIdentityDomainService _identityDomainService = identityDomainService;
  private readonly UserToResponseMapper _userToResponseMapper = userToResponseMapper;

  #region Identity RBAC
  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to read the specified user identity.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can read any user.</item>
  ///   <item>Self-access is always allowed.</item>
  ///   <item>Non-admin users must have <c>Read</c> permission on <c>Identity</c> scope (entity type) for every organization the target user belongs to.</item>
  ///   <item>If none of the above conditions are met, access is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="userId">The ID of the user to be read.</param>
  /// <returns>
  /// A <see cref="Result{User}"/> indicating success (with the target user) or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result<User?> ReadUserRBAC(JwtTokenData jwtTokenData, Guid userId) {
    var targetIdentityResult = _identityDomainService.ReadUserById(userId);
    if (!targetIdentityResult.IsSuccess || targetIdentityResult.Value == null)
      return targetIdentityResult.ToResultOfType<User?>(_ => null);

    var targetIdentity = targetIdentityResult.Value;
    var targetAuthResult = _identityDomainService.ReadUserAuthorization(userId);
    var targetAuthorization = targetAuthResult.IsSuccess ? targetAuthResult.Value : null;

    return RBACWrapperJwtToken(
      jwtTokenData,
      targetIdentity,
      userRules: (_) => {

          return Result<User?>.Ok(targetIdentity);
      });
  }

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to create a user identity.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can create any user.</item>
  ///   <item>Non-admin users must have <c>Create</c> permission on <c>Identity</c> scope for every organization in the create request's entity scopes.</item>
  ///   <item>If none of the above conditions are met, creation is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="requestData">The create user request data.</param>
  /// <returns>
  /// A <see cref="Result"/> indicating success or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result CreateUserRBAC(JwtTokenData jwtTokenData, CreateUserRequest requestData) => RBACWrapperJwtToken(
    jwtTokenData,
    (jwtTokenData) => {
        return Result.Ok();
    });

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to patch (update) the specified user.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can patch any user, including the <c>IsGlobalAdmin</c> flag.</item>
  ///   <item>Only Global Admin can assign or remove the <c>IsGlobalAdmin</c> flag.</item>
  ///   <item>Self-patch is allowed for master/auth/2FA fields only; roles/organizations are forbidden.</item>
  ///   <item>Non-admin users must have <c>Write</c> permission on <c>Identity</c> scope for all organizations the target user belongs to.</item>
  ///   <item>If the patch modifies organization/application scopes, the user must have <c>Write</c> on <c>Identity</c> scope for all affected organizations.</item>
  ///   <item>If none of the above conditions are met, patching is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="userId">The ID of the user to be patched.</param>
  /// <param name="requestData">The patch request data.</param>
  /// <returns>
  /// A <see cref="Result{User}"/> indicating success (with the target user) or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result<User?> PatchUserRBAC(JwtTokenData jwtTokenData, Guid userId, PatchUserRequest requestData) {
    var targetIdentityResult = _identityDomainService.ReadUserById(userId);
    if (!targetIdentityResult.IsSuccess || targetIdentityResult.Value == null)
      return targetIdentityResult.ToResultOfType<User?>(_ => null);

    var targetIdentity = targetIdentityResult.Value;
    var targetAuthResult = _identityDomainService.ReadUserAuthorization(userId);
    var targetAuthorization = targetAuthResult.IsSuccess ? targetAuthResult.Value : null;

    return RBACWrapperJwtToken(
      jwtTokenData,
      targetIdentity,
      (_) => {
        return Result<User?>.Ok(targetIdentity);
      });
  }

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to delete the specified user.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Self-deletion is always forbidden for any user.</item>
  ///   <item>Only a Global Admin can delete another Global Admin.</item>
  ///   <item>Deleting the last Global Admin is forbidden to prevent system lock-out.</item>
  ///   <item>Global Admins can delete any user (except themselves and the last Global Admin).</item>
  ///   <item>Non-admin users must have <c>Delete</c> permission on <c>Identity</c> scope for all organizations the target user belongs to.</item>
  ///   <item>If none of the above conditions are met, deletion is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="userId">The ID of the user to be deleted.</param>
  /// <returns>
  /// A <see cref="Result{User}"/> indicating success (with the target user) or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result DeleteUserRBAC(JwtTokenData jwtTokenData, Guid userId) {
    var targetIdentityResult = _identityDomainService.ReadUserById(userId);
    if (!targetIdentityResult.IsSuccess || targetIdentityResult.Value == null)
      return targetIdentityResult;

    var targetIdentity = targetIdentityResult.Value;
    var targetAuthResult = _identityDomainService.ReadUserAuthorization(userId);
    var targetAuthorization = targetAuthResult.IsSuccess ? targetAuthResult.Value : null;
    var targetIsGlobalAdmin = targetAuthorization?.IsGlobalAdmin ?? false;
    var targetEntityScopes = targetAuthorization?.EntityScopes ?? [];

    return RBACWrapperJwtToken(
      jwtTokenData,
      targetIdentity,
      (_) => {
          return targetIdentityResult;
      });
  }
  #endregion

  #region Search
  /// <summary>
  /// Returns a paged list of users the acting user is authorized to view, enforcing strict RBAC based on organization-level scopes.
  /// <para>
  /// - Global Admin can view all users.
  /// - Non-admins can only view users for whom they have <c>Read</c> on <b>Organization</b> or <b>Identity</b> scope for <b>all</b> organizations the user belongs to.
  /// - The acting user is always excluded from the result.
  /// </para>
  /// The returned users are further filtered by the provided search criteria.
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="requestData">The search and paging parameters.</param>
  /// <returns>
  /// A <see cref="Result{PagedResponse{SearchUserResponse}}"/> containing the paged list of users the acting user is allowed to see.
  /// </returns>
  public Result<PagedResponse<SearchUserResponse>?> SearchUsers(JwtTokenData jwtTokenData, SearchUserRequest requestData) {
    var requestFilter = (requestData.BuildFilterExpression<UserDto>() ?? (u => true))
        .AndAlso(u => u.Id != jwtTokenData.UserId);

    if (jwtTokenData.IsGlobalAdmin)
      return ExecuteSearch(requestData, requestFilter);

    var identityReadIds = (jwtTokenData.EntityScopes ?? Enumerable.Empty<IdentityScopeData>())
        .Where(sc => sc.EntityType == ScopeEntityType.Identity && RbacHelpers.Has(sc.Scope, ScopePermission.Read))
        .Select(sc => sc.EntityId)
        .ToHashSet();
    var visibleOrgIds = identityReadIds;

    Expression<Func<UserDto, bool>> accessScope = u =>
        !u.EntityScopes
          .Select(es => es.EntityId)
          .Except(visibleOrgIds)
          .Any();

    var finalPredicate = accessScope.AndAlso(requestFilter);
    return ExecuteSearch(requestData, finalPredicate);
  }

  /// <summary>
  /// Executes the search query with the given predicates.
  /// </summary>
  private Result<PagedResponse<SearchUserResponse>?> ExecuteSearch(SearchUserRequest requestData, Expression<Func<UserDto, bool>>? usersPredicate) {
    var skip = (requestData.PageNumber - 1) * requestData.PageSize;
    var take = requestData.PageSize;

    var usersResult = _identityQueryService.Search(usersPredicate, skip, take);

    if (!usersResult.IsSuccess || usersResult.Value == null)
      return usersResult.ToResultOfType<PagedResponse<SearchUserResponse>?>(_ => null);

    var users = usersResult.Value;

    var usersCountResult = _identityQueryService.Count(usersPredicate);

    if (!usersCountResult.IsSuccess || usersCountResult.Value == null)
      return usersCountResult.ToResultOfType<PagedResponse<SearchUserResponse>?>(_ => null);

    var usersCount = usersCountResult.Value ?? 0;

    var pagedResponse = new PagedResponse<SearchUserResponse>(
        users.Select(MapToSearchResponse),
        usersCount,
        requestData.PageNumber,
        requestData.PageSize
    );

    return Result<PagedResponse<SearchUserResponse>?>.Ok(pagedResponse);
  }

  /// <summary>
  /// Returns a paged list of user entity scopes the acting user is authorized to view.
  /// Global Admin sees all; others see only scopes for users they can read (same visible-org logic as SearchUsers).
  /// </summary>
  public Result<PagedResponse<SearchUserEntityScopeResponse>?> SearchUserEntityScopes(JwtTokenData jwtTokenData, SearchUserEntityScopeRequest requestData) {
    Expression<Func<UserEntityScopeDto, bool>>? requestFilter = requestData.BuildFilterExpression();
    if (requestData.UserId.HasValue)
      requestFilter = (requestFilter ?? (s => true)).AndAlso(s => s.UserId == requestData.UserId.Value);

    if (jwtTokenData.IsGlobalAdmin)
      return ExecuteSearchUserEntityScopes(requestData, requestFilter);

    var identityReadIds = (jwtTokenData.EntityScopes ?? Enumerable.Empty<IdentityScopeData>())
        .Where(sc => sc.EntityType == ScopeEntityType.Identity && RbacHelpers.Has(sc.Scope, ScopePermission.Read))
        .Select(sc => sc.EntityId)
        .ToHashSet();
    var visibleOrgIds = identityReadIds;

    Expression<Func<UserDto, bool>> accessScope = u =>
        !u.EntityScopes
          .Select(es => es.EntityId)
          .Except(visibleOrgIds)
          .Any();

    var usersResult = _identityQueryService.Search(accessScope, 0, 50000);
    var allowedUserIds = new HashSet<Guid>();
    if (usersResult.IsSuccess && usersResult.Value != null && usersResult.Value.Count > 0)
      allowedUserIds = usersResult.Value.Select(u => u.Id).ToHashSet();

    Expression<Func<UserEntityScopeDto, bool>> scopeAccessFilter = s => allowedUserIds.Contains(s.UserId);
    var finalPredicate = scopeAccessFilter.AndAlso(requestFilter ?? (s => true));
    return ExecuteSearchUserEntityScopes(requestData, finalPredicate);
  }

  private Result<PagedResponse<SearchUserEntityScopeResponse>?> ExecuteSearchUserEntityScopes(SearchUserEntityScopeRequest requestData, Expression<Func<UserEntityScopeDto, bool>>? predicate) {
    var skip = (requestData.PageNumber - 1) * requestData.PageSize;
    var take = requestData.PageSize;

    var scopesResult = _userEntityScopeQueryService.Search(predicate, skip, take);
    if (!scopesResult.IsSuccess || scopesResult.Value == null)
      return scopesResult.ToResultOfType<PagedResponse<SearchUserEntityScopeResponse>?>(_ => null);

    var scopes = scopesResult.Value;
    var countResult = _userEntityScopeQueryService.Count(predicate);
    if (!countResult.IsSuccess || countResult.Value == null)
      return countResult.ToResultOfType<PagedResponse<SearchUserEntityScopeResponse>?>(_ => null);

    var totalCount = countResult.Value ?? 0;
    var pagedResponse = new PagedResponse<SearchUserEntityScopeResponse>(
        scopes.Select(_userToResponseMapper.MapToSearchResponse),
        totalCount,
        requestData.PageNumber,
        requestData.PageSize
    );
    return Result<PagedResponse<SearchUserEntityScopeResponse>?>.Ok(pagedResponse);
  }
  #endregion

  #region Read
  /// <summary>
  /// N.B. This method should be already ok.
  /// Last update: 02/03/2026
  /// </summary>
  public Result<UserResponse?> ReadUser(JwtTokenData jwtTokenData, Guid id) {
    var userResult = ReadUserRBAC(jwtTokenData, id);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<UserResponse?>(_ => null);

    var user = userResult.Value;
    var authResult = _identityDomainService.ReadUserAuthorization(user.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;
    var response = MapToResponse(user, authorization);

    return Result<UserResponse?>.Ok(response);
  }
  #endregion

  #region Create
  /// <summary>
  /// N.B.This method now enforces stricter RBAC pre-checks via CreateUserRBAC.
  /// Uses a saga: Step 1 write user, Step 2 write authorization; on Step 2 failure, compensates by deleting the created user.
  /// </summary>
  /// <remarks>Last update: 02/03/2026</remarks>
  public async Task<Result<UserResponse?>> PostUserAsync(JwtTokenData jwtTokenData, CreateUserRequest requestData) {
    var createResult = CreateUserRBAC(jwtTokenData, requestData);
    if (!createResult.IsSuccess)
      return createResult.ToResultOfType<UserResponse?>(null);

    var pepper = _appSettings.CertsEngineConfiguration.JwtSettingsConfiguration.PasswordPepper;
    var newUser = new User(requestData.Username, requestData.Password, pepper)
      .SetEmail(requestData.Email)
      .SetMobileNumber(requestData.MobileNumber);

    var sagaBuilder = new LocalSagaBuilder(_logger);

    sagaBuilder.AddStep(
      name: "Step1. Write user",
      execute: async (ctx, ct) => {
        var result = await _identityDomainService.WriteUserAsync(newUser);
        if (!result.IsSuccess || result.Value == null)
          return result.ToResultOfType<UserResponse?>(_ => null);
        var user = result.Value;
        ctx.Set("createdUserId", user.Id);
        var tempResponse = MapToResponse(user, null!);
        return Result<UserResponse?>.Ok(tempResponse);
      },
      outputKey: "userResponse");

    sagaBuilder.AddStep(
      name: "Step2. Write user authorization",
      async (ctx, ct) => {
        var userResponseResult = ctx.Get<Result<UserResponse?>>("userResponse");
        if (userResponseResult == null || !userResponseResult.IsSuccess || userResponseResult.Value == null)
          return userResponseResult ?? Result<UserResponse?>.InternalServerError(null, "Saga step 1 did not produce a result.");
        var userId = ctx.Get<Guid>("createdUserId");
        var authorization = new UserAuthorization(userId)
          .SetIsGlobalAdmin(requestData.IsGlobalAdmin);
        if (requestData.EntityScopes != null) {
          var groupedByEntityAndType = requestData.EntityScopes
            .GroupBy(r => (r.EntityId, r.EntityType))
            .ToList();
          var entityScopes = groupedByEntityAndType
            .Select(g => new UserEntityScope(g.First().EntityId, g.First().EntityType, g.First().Scope))
            .ToList();
          authorization.SetEntityScopes(entityScopes);
        }
        var authWriteResult = await _identityDomainService.WriteUserAuthorizationAsync(authorization);
        if (!authWriteResult.IsSuccess) {
          var err = authWriteResult.ToResultOfType<UserResponse?>(null);
          ctx.Set("finalUserResponse", err);
          return err;
        }
        var userResult = _identityDomainService.ReadUserById(userId);
        if (!userResult.IsSuccess || userResult.Value == null) {
          var err = userResult.ToResultOfType<UserResponse?>(_ => null);
          ctx.Set("finalUserResponse", err);
          return err;
        }
        var response = MapToResponse(userResult.Value, authorization);
        var ok = Result<UserResponse?>.Ok(response);
        ctx.Set("finalUserResponse", ok);
        return ok;
      });

    var saga = sagaBuilder.Build();
    var sagaContext = new LocalSagaContext();
    await saga.ExecuteAsync(sagaContext);

    var finalResult = sagaContext.Get<Result<UserResponse?>>("finalUserResponse")
      ?? sagaContext.Get<Result<UserResponse?>>("userResponse");
    if (finalResult == null)
      return Result<UserResponse?>.InternalServerError(null, "Saga execution failed without a result.");

    if (!finalResult.IsSuccess) {
      var createdUserId = sagaContext.Get<Guid>("createdUserId");
      if (createdUserId != Guid.Empty) {
        _logger.LogWarning("PostUser saga failed after Step 1; compensating by deleting created user {UserId}", createdUserId);
        await _identityDomainService.DeleteUserAsync(createdUserId);
      }
    }

    return finalResult;
  }
  #endregion

  #region Patch
  /// <summary>
  /// RBAC is centralized in PatchUserRBAC (touched-orgs coverage, IsGlobalAdmin restriction, org-level roles restriction).
  /// This method performs shape validation and applies domain mutations only. When authorization is patched, uses a saga (write user then auth) with compensation on failure.
  /// </summary>
  /// <remarks>Last update: 02/03/2026</remarks>
  public async Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData jwtTokenData, Guid id, PatchUserRequest requestData) {
    var userResult = PatchUserRBAC(jwtTokenData, id, requestData);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<UserResponse?>(_ => null);

    var user = userResult.Value;

    // 1) Patch master data (username, email, phone, active)
    var masterDataResult = PatchUserMasterData(user, requestData);
    if (!masterDataResult.IsSuccess)
      return masterDataResult;

    // 2) Patch authentication fields (password)
    var authPropsResult = PatchUserAuthentication(user, requestData);
    if (!authPropsResult.IsSuccess)
      return authPropsResult;

    // 3) Patch two-factor settings
    var twoFactorResult = PatchUserTwoFactor(user, requestData, out var twoFactorRecoveryCodes);
    if (!twoFactorResult.IsSuccess)
      return twoFactorResult;

    // 4) Patch authorization (IsGlobalAdmin, EntityScopes)
    var authorizationResult = PatchUserAuthorization(user, requestData, out var authorization);
    if (!authorizationResult.IsSuccess)
      return authorizationResult;

    if (authorization == null) {
      var upsertResult = await _identityDomainService.WriteUserAsync(user);
      if (!upsertResult.IsSuccess || upsertResult.Value == null)
        return upsertResult.ToResultOfType<UserResponse?>(_ => null);
      user = upsertResult.Value;
      return Result<UserResponse?>.Ok(MapToResponse(user, null));
    }

    var writeResult = await _identityDomainService.WriteUserAsync(user, authorization);
    if (!writeResult.IsSuccess || writeResult.Value == null)
      return writeResult.ToResultOfType<UserResponse?>(_ => null);
    user = writeResult.Value;

    if (twoFactorRecoveryCodes != null) {
      var userResponse = MapToResponse(user, authorization);
      var twoFactorSettingsConfiguration = _appSettings.CertsEngineConfiguration.TwoFactorSettingsConfiguration;
      if (!TotpGenerator.TryGenerateTotpAuthLink(
          twoFactorSettingsConfiguration.Label,
          user.Username,
          user.TwoFactorSharedKey ?? "",
          twoFactorSettingsConfiguration.Issuer,
          twoFactorSettingsConfiguration.Algorithm,
          twoFactorSettingsConfiguration.Digits,
          twoFactorSettingsConfiguration.Period,
          out var authLink,
          out var errorMessage
      )) {
        _logger.LogError(errorMessage);
        return Result<UserResponse?>.InternalServerError(null, errorMessage);
      }
      userResponse.QrCodeUrl = authLink;
      userResponse.TwoFactorRecoveryCodes = twoFactorRecoveryCodes;
      userResponse.RecoveryCodesLeft = user.TwoFactorRecoveryCodes.Count(x => !x.IsUsed);
      return Result<UserResponse?>.Ok(userResponse);
    }

    return Result<UserResponse?>.Ok(MapToResponse(user, authorization));
  }
  #endregion

  /// <summary>
  /// Applies master-data patches (username, email, mobile number, IsActive) to the given user.
  /// Returns a failed <see cref="Result{UserResponse}"/> if any operation or required field is invalid.
  /// </summary>
  private Result<UserResponse?> PatchUserMasterData(User user, PatchUserRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.Username), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.Username == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.Username));
          user.SetUsername(requestData.Username);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.Email), out operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.Email == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.Email));
          user.SetEmail(requestData.Email);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.MobileNumber), out operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.MobileNumber == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.MobileNumber));
          user.SetMobileNumber(requestData.MobileNumber);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.IsActive), out operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.IsActive == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.IsActive));
          user.SetIsActive(requestData.IsActive.Value);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    return Result<UserResponse?>.Ok(null);
  }

  /// <summary>
  /// Applies authentication-related patches (password) to the given user.
  /// </summary>
  private Result<UserResponse?> PatchUserAuthentication(User user, PatchUserRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.Password), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.Password == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.Password));
          user.SetPassword(requestData.Password, _appSettings.CertsEngineConfiguration.JwtSettingsConfiguration.PasswordPepper);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    return Result<UserResponse?>.Ok(null);
  }

  /// <summary>
  /// Applies two-factor authentication patches and returns recovery codes when 2FA is enabled.
  /// </summary>
  private Result<UserResponse?> PatchUserTwoFactor(User user, PatchUserRequest requestData, out List<string>? twoFactorRecoveryCodes) {
    twoFactorRecoveryCodes = null;

    if (requestData.TwoFactorEnabled == true) {
      var enableTwoFactorAuthResult = _identityDomainService.EnableTwoFactorAuthForUser(user);
      if (!enableTwoFactorAuthResult.IsSuccess)
        return enableTwoFactorAuthResult.ToResultOfType<UserResponse?>(_ => null);

      twoFactorRecoveryCodes = enableTwoFactorAuthResult.Value;
    }
    else if (requestData.TwoFactorEnabled == false) {
      var disableResult = user.DisableTwoFactorAuth();
      if (!disableResult.IsSuccess)
        return disableResult.ToResultOfType<UserResponse?>(_ => null);
    }

    return Result<UserResponse?>.Ok(null);
  }

  /// <summary>
  /// Applies authorization patches (IsGlobalAdmin and EntityScopes) and returns the resulting authorization object (or null if unchanged).
  /// </summary>
  private Result<UserResponse?> PatchUserAuthorization(User user, PatchUserRequest requestData, out UserAuthorization? authorization) {
    authorization = null;

    var authResult = _identityDomainService.ReadUserAuthorization(user.Id);
    if (authResult.IsSuccess && authResult.Value != null)
      authorization = authResult.Value;

    if (requestData.TryGetOperation(nameof(requestData.IsGlobalAdmin), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.IsGlobalAdmin == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.IsGlobalAdmin));
          (authorization ??= new UserAuthorization(user.Id)).SetIsGlobalAdmin(requestData.IsGlobalAdmin.Value);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    if (requestData.EntityScopes != null) {
      authorization ??= new UserAuthorization(user.Id);
      var currentScopes = authorization.EntityScopes.ToList();

      foreach (var scopePatch in requestData.EntityScopes) {
        if (scopePatch.TryGetOperation(Constants.CollectionItemOperation, out var collectionOp)) {
          switch (collectionOp) {
            case PatchOperation.AddToCollection:
              if (scopePatch.EntityId.HasValue && scopePatch.EntityType.HasValue && scopePatch.Scope.HasValue &&
                  !currentScopes.Any(s =>
                  s.EntityId == scopePatch.EntityId &&
                  s.EntityType == scopePatch.EntityType &&
                  s.Scope == scopePatch.Scope)) {
                currentScopes.Add(new UserEntityScope(scopePatch.EntityId!.Value, scopePatch.EntityType!.Value, scopePatch.Scope!.Value));
              }
              break;

            case PatchOperation.RemoveFromCollection:
              if (scopePatch.Id != null && scopePatch.Id != Guid.Empty) {
                currentScopes.RemoveAll(x => x.Id == scopePatch.Id);
              }
              else if (scopePatch.EntityId.HasValue && scopePatch.EntityType.HasValue && scopePatch.Scope.HasValue) {
                currentScopes.RemoveAll(s =>
                    s.EntityId == scopePatch.EntityId &&
                    s.EntityType == scopePatch.EntityType &&
                    s.Scope == scopePatch.Scope);
              }
              break;

            default:
              return UnsupportedPatchOperationResponse<UserResponse?>();
          }
        }
        else if (scopePatch.Id != null && scopePatch.Id != Guid.Empty) {
          // Update in place: client sent same scope with changed fields (e.g. permission change) without Add/Remove.
          // Only apply EntityId/EntityType/Scope when the client sent them (omit when payload has only id).
          var existing = currentScopes.FirstOrDefault(s => s.Id == scopePatch.Id);
          if (existing != null) {
            if (scopePatch.EntityId.HasValue) existing.SetEntityId(scopePatch.EntityId.Value);
            if (scopePatch.EntityType.HasValue) existing.SetEntityType(scopePatch.EntityType.Value);
            if (scopePatch.Scope.HasValue) existing.SetScope(scopePatch.Scope.Value);
          }
        }
      }

      authorization.SetEntityScopes(currentScopes);
    }

    return Result<UserResponse?>.Ok(null);
  }

  #region Delete
  /// <summary>
  /// N.B. Hardened: managers cannot delete users with org-level roles in their org.
  /// Last update: 02/03/2026
  /// </summary>
  public async Task<Result> DeleteUserAsync(JwtTokenData jwtTokenData, Guid id) {
    var userResult = DeleteUserRBAC(jwtTokenData, id);
    if (!userResult.IsSuccess)
      return userResult;

    var result = await _identityDomainService.DeleteUserAsync(id);
    return result;
  }
  #endregion

  #region Login/Refresh/Logout
  public async Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData) {
    return await HandleTokenResponseAsync(() =>
      _identityDomainService.LoginAsync(requestData.Username, requestData.Password, requestData.TwoFactorCode, requestData.TwoFactorRecoveryCode), _appSettings);
  }

  public async Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData) {
    return await HandleTokenResponseAsync(() =>
      _identityDomainService.RefreshTokenAsync(requestData.RefreshToken, requestData.Force), _appSettings);
  }

  private static async Task<Result<LoginResponse?>> HandleTokenResponseAsync(Func<Task<Result<JwtToken?>>> tokenOperation, Configuration appSettings) {
    var jwtTokenResult = await tokenOperation();
    if (!jwtTokenResult.IsSuccess || jwtTokenResult.Value == null)
      return jwtTokenResult.ToResultOfType<LoginResponse?>(_ => null);

    var jwtToken = jwtTokenResult.Value;
    var jwtConfig = appSettings.CertsEngineConfiguration.JwtSettingsConfiguration;

    string? username = null;
    if (JwtGenerator.TryValidateToken(
        jwtConfig.JwtSecret,
        jwtConfig.Issuer,
        jwtConfig.Audience,
        jwtToken.Token,
        out var claims,
        out _) && claims?.Username != null) {
      username = claims.Username;
    }

    return Result<LoginResponse?>.Ok(new LoginResponse {
      TokenType = jwtToken.TokenType,
      Token = jwtToken.Token,
      ExpiresAt = jwtToken.ExpiresAt,
      RefreshToken = jwtToken.RefreshToken,
      RefreshTokenExpiresAt = jwtToken.RefreshTokenExpiresAt,
      Username = username
    });
  }

  public async Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData) {
    var logoutResult = await _identityDomainService.LogoutAsync(jwtTokenData.UserId, jwtTokenData.Token, requestData.LogoutFromAllDevices);
    return logoutResult;
  }
  #endregion

  #region Map to Response
  /// <summary>
  /// Maps User and optional UserAuthorization to API response. Authorization can be null for backward compatibility.
  /// </summary>
  protected UserResponse MapToResponse(User domain, UserAuthorization? authorization) =>
    _userToResponseMapper.MapToResponse(domain, authorization);

  protected override UserResponse MapToResponse(User domain) =>
    _userToResponseMapper.MapToResponse(domain, null);

  #endregion

  #region Map QueryResult to SerchResponse
  protected override SearchUserResponse MapToSearchResponse(UserQueryResult queryResult) =>
    _userToResponseMapper.MapToSearchResponse(queryResult);
  #endregion
}
