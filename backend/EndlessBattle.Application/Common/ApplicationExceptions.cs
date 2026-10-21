namespace EndlessBattle.Application.Common;

// Api 層把這些例外對應成 HTTP 狀態碼：409、404、400、401
public sealed class ConflictException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);

public sealed class InputValidationException(string message) : Exception(message);

public sealed class AuthenticationFailedException() : Exception("帳號或密碼錯誤");
