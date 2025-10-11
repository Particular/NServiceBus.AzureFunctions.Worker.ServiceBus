using System;

// we can't inherit from the FunctionAttribute since the native source gen won't trigger then
public class NServiceBusFunctionAttribute() : Attribute;