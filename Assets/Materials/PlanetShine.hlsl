// how much of the planet's day side can we see?
float dayAngle(float2 p, float2 L, float R) {
  float2 P = float2(-L.y, L.x);
                
  float2 pos = float2(dot(L, IN.position), dot(P, IN.position));
  pos.y = abs(pos.y);

  // angle from planet center to horizon
  float theta_tangent = asin(R / length(pos));
  if (pos.y < R) {
      if (pos.x <= 0) return 0;
      else return theta_tangent * 2;
  } else {
      // view angle to planet center
      float theta_center = atan2(-pos.y, -pos.x);
      // view angle to illuminated horizon
      float theta_horiz = theta_center + theta_tangent;
      // view angle to day-night terminator
      float theta_term = atan2(R - pos.y, -pos.x);
      return fmod(theta_horiz - theta_term, 2 * PI);
  }
}