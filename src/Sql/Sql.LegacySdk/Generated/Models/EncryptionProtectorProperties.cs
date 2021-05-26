// 
// Copyright (c) Microsoft and contributors.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// 
// See the License for the specific language governing permissions and
// limitations under the License.
// 

// Warning: This code was generated by a tool.
// 
// Changes to this file may cause incorrect behavior and will be lost if the
// code is regenerated.

using System;
using System.Linq;

namespace Microsoft.Azure.Management.Sql.LegacySdk.Models
{
    /// <summary>
    /// Represents the properties for an Azure Sql Database Transparent Data
    /// Encryption Encryption Protector.
    /// </summary>
    public partial class EncryptionProtectorProperties
    {
        private string _serverKeyName;
        
        /// <summary>
        /// Optional. The name of the Server Key.
        /// </summary>
        public string ServerKeyName
        {
            get { return this._serverKeyName; }
            set { this._serverKeyName = value; }
        }
        
        private string _serverKeyType;
        
        /// <summary>
        /// Optional. The Transparent Data Encryption Encryption Protector Type
        /// </summary>
        public string ServerKeyType
        {
            get { return this._serverKeyType; }
            set { this._serverKeyType = value; }
        }
        
        private string _uri;
        
        /// <summary>
        /// Optional. The Uri of the Encryption Protector
        /// </summary>
        public string Uri
        {
            get { return this._uri; }
            set { this._uri = value; }
        }

        private bool? _isAutoRotationEnabled;

        /// <summary>
        /// Optional. Gets or sets the Azure Sql Server Encryption
        /// Protector Key Rotation Status
        /// </summary>
        public bool? AutoRotationEnabled
        {
            get { return this._isAutoRotationEnabled; }
            set { this._isAutoRotationEnabled = value; }
        }

        /// <summary>
        /// Initializes a new instance of the EncryptionProtectorProperties
        /// class.
        /// </summary>
        public EncryptionProtectorProperties()
        {
        }
    }
}
