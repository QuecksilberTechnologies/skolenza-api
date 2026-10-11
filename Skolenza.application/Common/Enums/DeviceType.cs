using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Common.Enums
{
    // ================================================================
    // Author  : Deepesh Gupta
    // Company : Quecksilber Technologies
    // Role    : CEO
    // Purpose : Defines supported biometric and attendance device types
    //           used by the application/API layer.
    // ================================================================

    

    #region Device Type Enum

    /// <summary>
    /// Defines the primary type/category of a biometric or attendance device.
    /// The numeric values are persisted as SMALLINT in the database.
    /// </summary>
    public enum DeviceType : short
    {
        /// <summary>
        /// Face recognition based biometric device.
        /// </summary>
        FaceRecognition = 1,

        /// <summary>
        /// Fingerprint based biometric device.
        /// </summary>
        Fingerprint = 2,

        /// <summary>
        /// RFID, proximity, or smart-card based device.
        /// </summary>
        Card = 3,

        /// <summary>
        /// Device supporting multiple biometric/authentication methods,
        /// such as face, fingerprint, card, PIN, or QR code.
        /// </summary>
        MultiBiometric = 4,

        /// <summary>
        /// Dedicated attendance terminal.
        /// </summary>
        AttendanceTerminal = 5,

        /// <summary>
        /// Dedicated access-control terminal.
        /// </summary>
        AccessControl = 6,

        /// <summary>
        /// Combined attendance and access-control device.
        /// </summary>
        AttendanceAndAccessControl = 7
    }

    #endregion
}
