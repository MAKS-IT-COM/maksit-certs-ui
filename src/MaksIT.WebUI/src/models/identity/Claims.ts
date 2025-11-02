export enum Claims {
  //
  // Summary:
  //     The URI for a claim that specifies the actor, http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor.
    Actor = 'http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor',
  //
  // Summary:
  //     The URI for a claim that specifies the postal code of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode.
  PostalCode = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode',
  //
  // Summary:
  //     The URI for a claim that specifies the primary group SID of an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid.
  PrimaryGroupSid = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid',
  //
  // Summary:
  //     The URI for a claim that specifies the primary SID of an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid.
  PrimarySid = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid',
  //
  // Summary:
  //     The URI for a claim that specifies the role of an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/role.
  Role = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  //
  // Summary:
  //     The URI for a claim that specifies an RSA key, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa.
  Rsa = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa',
  //
  // Summary:
  //     The URI for a claim that specifies a serial number, http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber.
  SerialNumber = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber',
  //
  // Summary:
  //     The URI for a claim that specifies a security identifier (SID), http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid.
  Sid = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid',
  //
  // Summary:
  //     The URI for a claim that specifies a service principal name (SPN) claim, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn.
  Spn = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn',
  //
  // Summary:
  //     The URI for a claim that specifies the state or province in which an entity resides,
  //     http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince.
  StateOrProvince = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince',
  //
  // Summary:
  //     The URI for a claim that specifies the street address of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress.
  StreetAddress = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress',
  //
  // Summary:
  //     The URI for a claim that specifies the surname of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname.
  Surname = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname',
  //
  // Summary:
  //     The URI for a claim that identifies the system entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system.
  System = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system',
  //
  // Summary:
  //     The URI for a claim that specifies a thumbprint, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint.
  //     A thumbprint is a globally unique SHA-1 hash of an X.509 certificate.
  Thumbprint = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint',
  //
  // Summary:
  //     The URI for a claim that specifies a user principal name (UPN), http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn.
  Upn = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn',
  //
  // Summary:
  //     The URI for a claim that specifies a URI, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri.
  Uri = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri',
  //
  // Summary:
  //     The URI for a claim that specifies the user data, http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata.
  UserData = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata',
  //
  // Summary:
  //     The URI for a claim that specifies the version, http://schemas.microsoft.com/ws/2008/06/identity/claims/version.
  Version = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/version',
  //
  // Summary:
  //     The URI for a claim that specifies the webpage of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage.
  Webpage = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage',
  //
  // Summary:
  //     The URI for a claim that specifies the Windows domain account name of an entity,
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname.
  WindowsAccountName = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim.
  WindowsDeviceClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim',
  //
  // Summary:
  //     The URI for a claim that specifies the Windows group SID of the device, http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup.
  WindowsDeviceGroup = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsfqbnversion.
  WindowsFqbnVersion = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsfqbnversion',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority.
  WindowsSubAuthority = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority',
  //
  // Summary:
  //     The URI for a claim that specifies the alternative phone number of an entity,
  //     http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone.
  OtherPhone = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone',
  //
  // Summary:
  //     The URI for a claim that specifies the name of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier.
  NameIdentifier = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  //
  // Summary:
  //     The URI for a claim that specifies the name of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name.
  Name = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  //
  // Summary:
  //     The URI for a claim that specifies the mobile phone number of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone.
  MobilePhone = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone',
  //
  // Summary:
  //     The URI for a claim that specifies the anonymous user, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous.
  Anonymous = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous',
  //
  // Summary:
  //     The URI for a claim that specifies details about whether an identity is authenticated,
  //     http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authenticated.
  Authentication = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication',
  //
  // Summary:
  //     The URI for a claim that specifies the instant at which an entity was authenticated,
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant.
  AuthenticationInstant = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant',
  //
  // Summary:
  //     The URI for a claim that specifies the method with which an entity was authenticated,
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod.
  AuthenticationMethod = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod',
  //
  // Summary:
  //     The URI for a claim that specifies an authorization decision on an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision.
  AuthorizationDecision = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision',
  //
  // Summary:
  //     The URI for a claim that specifies the cookie path, http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath.
  CookiePath = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath',
  //
  // Summary:
  //     The URI for a claim that specifies the country/region in which an entity resides,
  //     http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country.
  Country = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country',
  //
  // Summary:
  //     The URI for a claim that specifies the date of birth of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth.
  DateOfBirth = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth',
  //
  // Summary:
  //     The URI for a claim that specifies the deny-only primary group SID on an entity,
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid.
  //     A deny-only SID denies the specified entity to a securable object.
  DenyOnlyPrimaryGroupSid = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid',
  //
  // Summary:
  //     The URI for a claim that specifies the deny-only primary SID on an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid.
  //     A deny-only SID denies the specified entity to a securable object.
  DenyOnlyPrimarySid = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid',
  //
  // Summary:
  //     The URI for a claim that specifies a deny-only security identifier (SID) for
  //     an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid.
  //     A deny-only SID denies the specified entity to a securable object.
  DenyOnlySid = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim.
  WindowsUserClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim',
  //
  // Summary:
  //     The URI for a claim that specifies the Windows deny-only group SID of the device,
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup.
  DenyOnlyWindowsDeviceGroup = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa.
  Dsa = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa',
  //
  // Summary:
  //     The URI for a claim that specifies the email address of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress.
  Email = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration.
  Expiration = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/expired.
  Expired = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/expired',
  //
  // Summary:
  //     The URI for a claim that specifies the gender of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender.
  Gender = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender',
  //
  // Summary:
  //     The URI for a claim that specifies the given name of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname.
  GivenName = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname',
  //
  // Summary:
  //     The URI for a claim that specifies the SID for the group of an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid.
  GroupSid = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid',
  //
  // Summary:
  //     The URI for a claim that specifies a hash value, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash.
  Hash = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash',
  //
  // Summary:
  //     The URI for a claim that specifies the home phone number of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone.
  HomePhone = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone',
  //
  // Summary:
  //     http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent.
  IsPersistent = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent',
  //
  // Summary:
  //     The URI for a claim that specifies the locale in which an entity resides, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality.
  Locality = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality',
  //
  // Summary:
  //     The URI for a claim that specifies the DNS name associated with the computer
  //     name or with the alternative name of either the subject or issuer of an X.509
  //     certificate, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns.
  Dns = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns',
  //
  // Summary:
  //     The URI for an X.500 distinguished name claim, such as the subject of an X.509
  //     Public Key Certificate or an entry identifier in a directory services Directory
  //     Information Tree, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname.
  X500DistinguishedName = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname',


  AclEntry = 'acl_entry'
}
