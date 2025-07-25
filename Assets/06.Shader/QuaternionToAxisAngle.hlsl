void QuaternionToAxisAngle_float(float4 Quat, out float3 Axis, out float Angle)
{
    Quat = normalize(Quat);

    if (Quat.w > 0.999999)
    {
        Axis = float3(0, 1, 0); 
        Angle = 0;
    }
    else
    {
        float angleRad = 2.0 * acos(Quat.w);
        Axis = Quat.xyz / sqrt(1.0 - Quat.w * Quat.w);
        Angle = angleRad * 57.295779513;
    }
}