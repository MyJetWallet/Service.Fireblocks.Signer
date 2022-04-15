﻿namespace Service.Fireblocks.Signer.Grpc.Models.Common
{
    public enum ErrorCode
    {
        Unknown,
        AlreadyExist,
        DoesNotExist,
        ApiError,
        NotEnoughBalance,
        NoKey,
        WrongSignature,
        SignatureExpired
    }
}
