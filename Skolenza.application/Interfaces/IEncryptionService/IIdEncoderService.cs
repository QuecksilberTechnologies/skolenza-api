using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IEncryptionService
{
    public interface IIdEncoderService
    {


            string EncodeId_long(long id, string institutionKey);
             long DecodeId_long(string? encodedId, string institutionKey);
             int DecodeId_int(string? encodedId, string institutionKey);
              string EncodeId_int(int id, string institutionKey);
            string EncodeString(string input, string institutionKey);
            string DecodeString(string input, string institutionKey);

        


    }
}
