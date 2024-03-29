﻿// <auto-generated/>

namespace Sharpnado.MaterialFrame.Droid.BlurView
{
    public interface IBlurViewFacade
    {
        /**
     * Enables/disables the blur. Enabled by default
     *
     * @param enabled true to enable, false otherwise
     * @return {@link BlurViewFacade}
     */
        IBlurViewFacade SetBlurEnabled(bool enabled);

        /**
     * Can be used to stop blur auto update or resume if it was stopped before.
     * Enabled by default.
     *
     * @return {@link BlurViewFacade}
     */
        IBlurViewFacade SetBlurAutoUpdate(bool enabled);

        /**
     * Can be set to true to optimize position calculation before blur.
     * By default, BlurView calculates its translation, rotation and scale before each draw call.
     * If you are not changing these properties (for example, during animation), this behavior can be changed
     * to calculate them only once during initialization.
     *
     * @param hasFixedTransformationMatrix indicates if this BlurView has fixed transformation Matrix.
     * @return {@link BlurViewFacade}
     */
        IBlurViewFacade SetHasFixedTransformationMatrix(bool hasFixedTransformationMatrix);


        /**
     * @param radius sets the blur radius
     *               Default value is {@link BlurController#DEFAULT_BLUR_RADIUS}
     * @return {@link BlurViewFacade}
     */
        IBlurViewFacade SetBlurRadius(float radius);

        /**
     * Sets the color overlay to be drawn on top of blurred content
     *
     * @param overlayColor int color
     * @return {@link BlurViewFacade}
     */
        IBlurViewFacade SetOverlayColor(int overlayColor);
    }
}