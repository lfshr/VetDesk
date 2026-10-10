namespace SimpleResult;

public readonly struct FailureResult
{
    public IReadOnlyList<string>? Errors { get; init; }
    public Exception? Exception { get; init; }
    public object? ErrorCode { get; init; }
}

public class Result
{
    public bool IsSuccess => (Errors is null || Errors.Count == 0) && Exception is null;
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string>? Errors { get; protected init; }
    public Exception? Exception { get; protected init; }
    public object? ErrorCode { get; protected init; }

    public static Result Success()
    {
        return new Result();
    }

    // Accept implicit generics
    public static Result<T> Success<T>(T result) => Result<T>.Success(result);

    public static FailureResult Failure(Exception exception)
    {
        return new FailureResult
        {
            Exception = exception,
            Errors = [exception.Message]
        };
    }

    public static FailureResult Failure(params string[] errors)
    {
        return new FailureResult
        {
            Errors = errors.ToList()
        };
    }

    public static FailureResult Failure<TCode>(TCode code, params string[] errors)
        where TCode : struct, Enum
    {
        return new FailureResult
        {
            ErrorCode = code,
            Errors = errors.ToList()
        };
    }

    public static FailureResult Failure<TCode>(TCode code, Exception exception)
        where TCode : struct, Enum
    {
        return new FailureResult
        {
            ErrorCode = code,
            Exception = exception,
            Errors = [exception.Message]
        };
    }

    public static implicit operator Result(FailureResult f)
    {
        return new Result
        {
            Errors = f.Errors,
            Exception = f.Exception,
            ErrorCode = f.ErrorCode
        };
    }

    public void ThrowIfFailure()
    {
        if (!IsFailure) return;

        if (Exception is not null)
        {
            throw Exception;
        }

        throw new AggregateException(Errors.Select(e => new SimpleResultException(e)));
    }

    public override string ToString()
    {
        if (Errors is not null && Errors.Any())
        {
            return string.Join('\n', Errors);
        }

        if (Exception is not null)
        {
            return Exception.Message;
        }

        return "Success";
    }
}

public class Result<T> : Result
{
    public T Value { get; protected set; }

    public static implicit operator Result<T>(FailureResult f)
    {
        return new Result<T>
        {
            Errors = f.Errors,
            Exception = f.Exception,
            ErrorCode = f.ErrorCode,
            Value = default
        };
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>
        {
            Value = value
        };
    }

    public static Result<T> Failure(Exception exception, T value = default)
    {
        return new Result<T>
        {
            Exception = exception,
            Errors = [exception.Message],
            Value = value
        };
    }

    public static Result<T> Failure(T value, params string[] errors)
    {
        return new Result<T>
        {
            Errors = errors.ToList(),
            Value = value
        };
    }

    public new static Result<T> Failure(params string[] errors)
    {
        return new Result<T>
        {
            Errors = errors.ToList()
        };
    }
}
